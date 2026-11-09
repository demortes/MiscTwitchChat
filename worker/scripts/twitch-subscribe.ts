// Manage channel.chat.message EventSub webhook subscriptions.
//
// Run with Node >= 22.18 (type stripping is on by default; Node 22.6-22.17 needs
// `--experimental-strip-types`):
//   node scripts/twitch-subscribe.ts <channel...>         create subscriptions
//   node scripts/twitch-subscribe.ts --list               list subscriptions
//   node scripts/twitch-subscribe.ts --delete <id|all>    delete one or all
//   node scripts/twitch-subscribe.ts --auth <login...>    show which scopes accounts granted the app
//
// Anything the script needs is asked for interactively (secrets are not echoed), unless it is
// already in the environment, which is how to run it without a terminal:
//   TWITCH_CLIENT_ID, TWITCH_CLIENT_SECRET            your app, from dev.twitch.tv/console
//   TWITCH_EVENTSUB_SECRET                            same value as the Worker secret (10-100 chars)
//   TWITCH_CALLBACK_URL                               https://<your-worker-domain>/twitch/eventsub
//   TWITCH_BOT_USER_ID                                bot account's numeric user id, or its login name
//                                                     (that account must have authorized the app with
//                                                     user:read:chat -- see docs/twitch-eventsub.md)
// Channel args are logins (e.g. somestreamer); they are resolved to numeric ids via Helix.

declare const process: {
  argv: string[];
  env: Record<string, string | undefined>;
  exit(code: number): never;
  stdin: any;
  stdout: { write(s: string): unknown };
};

const HELIX = 'https://api.twitch.tv/helix';

// Reads one line from the terminal. Secrets are never echoed back.
function readLine(label: string, secret: boolean): Promise<string> {
  return new Promise((resolve) => {
    const { stdin, stdout } = process;
    let buf = '';
    stdout.write(label);
    stdin.setRawMode(true);
    stdin.setEncoding('utf8');
    stdin.resume();
    const finish = () => {
      stdin.off('data', onData);
      stdin.setRawMode(false);
      stdin.pause();
      stdout.write('\n');
      resolve(buf.trim());
    };
    const onData = (chunk: string) => {
      if (chunk.startsWith('\x1b')) return; // arrow keys and other escape sequences
      for (const ch of chunk) {
        if (ch === '\r' || ch === '\n') return finish();
        if (ch === '\x03') {
          stdout.write('\n');
          process.exit(130);
        }
        if (ch === '\x7f' || ch === '\b') {
          if (buf) {
            buf = buf.slice(0, -1);
            if (!secret) stdout.write('\b \b');
          }
          continue;
        }
        if (ch < ' ') continue;
        buf += ch;
        if (!secret) stdout.write(ch);
      }
    };
    stdin.on('data', onData);
  });
}

type Ask = { secret?: boolean; validate?: (v: string) => string | null };

// Value from the environment if set, otherwise asked for. Without a terminal, a missing value is an error.
async function get(name: string, label: string, opts: Ask = {}): Promise<string> {
  const fromEnv = process.env[name]?.trim();
  if (fromEnv) {
    const err = opts.validate?.(fromEnv);
    if (err) {
      console.error(`${name}: ${err}`);
      process.exit(1);
    }
    return fromEnv;
  }
  if (!process.stdin.isTTY) {
    console.error(`Missing env var ${name}`);
    process.exit(1);
  }
  for (;;) {
    const v = await readLine(`${label}: `, !!opts.secret);
    const err = v ? (opts.validate?.(v) ?? null) : 'A value is required.';
    if (!err) return v;
    console.error(err);
  }
}

const eventSubSecret: Ask = {
  secret: true,
  validate: (v) => (v.length >= 10 && v.length <= 100 ? null : 'Must be 10-100 characters.'),
};
const callbackUrl: Ask = {
  validate: (v) => (/^https:\/\/[^\s/]+\/\S*$/.test(v) ? null : 'Must be an https:// URL including the path, e.g. https://example.com/twitch/eventsub'),
};

async function appToken(id: string, secret: string): Promise<string> {
  const res = await fetch('https://id.twitch.tv/oauth2/token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({ client_id: id, client_secret: secret, grant_type: 'client_credentials' }),
  });
  if (!res.ok) throw new Error(`token request failed: ${res.status} ${await res.text()}`);
  return ((await res.json()) as { access_token: string }).access_token;
}

async function helix(path: string, token: string, clientId: string, init: RequestInit = {}) {
  const res = await fetch(HELIX + path, {
    ...init,
    headers: { Authorization: `Bearer ${token}`, 'Client-Id': clientId, 'Content-Type': 'application/json' },
  });
  const text = await res.text();
  if (!res.ok) throw new Error(`${init.method ?? 'GET'} ${path} -> ${res.status} ${text}`);
  return text ? JSON.parse(text) : null;
}

async function listAll(token: string, clientId: string): Promise<any[]> {
  const out: any[] = [];
  let cursor = '';
  do {
    const r = await helix('/eventsub/subscriptions' + (cursor ? `?after=${cursor}` : ''), token, clientId);
    out.push(...r.data);
    cursor = r.pagination?.cursor ?? '';
  } while (cursor);
  return out;
}

async function main() {
  const args = process.argv.slice(2);
  const channels = args.filter((a) => !a.startsWith('--')).map((c) => c.replace(/^#/, '').toLowerCase());
  const creating = args[0] !== '--list' && args[0] !== '--delete' && args[0] !== '--auth';
  if (creating && channels.length === 0) {
    throw new Error('Usage: twitch-subscribe.ts <channel...> | --list | --delete <id|all> | --auth <login...>');
  }
  if (args[0] === '--delete' && !args[1]) throw new Error('--delete requires <id|all>');
  if (args[0] === '--auth' && channels.length === 0) throw new Error('--auth requires at least one account login');

  const clientId = await get('TWITCH_CLIENT_ID', 'Twitch app client ID');
  const clientSecret = await get('TWITCH_CLIENT_SECRET', 'Twitch app client secret', { secret: true });
  const token = await appToken(clientId, clientSecret);

  if (args[0] === '--list') {
    for (const s of await listAll(token, clientId)) {
      console.log(`${s.id}  ${s.status}  ${s.type}  ${JSON.stringify(s.condition)}  ${s.transport.callback ?? ''}`);
    }
    return;
  }

  if (args[0] === '--auth') {
    // The bot account needs both of these for a channel.chat.message webhook (see docs/twitch-eventsub.md).
    const needed = ['user:read:chat', 'user:bot'];
    const q = channels.map((c) => `login=${encodeURIComponent(c)}`).join('&');
    const users = (await helix(`/users?${q}`, token, clientId)).data as { id: string; login: string }[];
    const granted = new Map<string, string[]>();
    if (users.length) {
      const r = await helix(`/authorization/users?${users.map((u) => `user_id=${u.id}`).join('&')}`, token, clientId);
      for (const a of r.data as { user_id: string; scopes?: string[]; has_authorization?: boolean }[]) {
        if (a.has_authorization !== false) granted.set(a.user_id, a.scopes ?? []);
      }
    }
    for (const login of channels) {
      const u = users.find((x) => x.login === login);
      if (!u) console.log(`${login}: no such Twitch account`);
      else if (!granted.has(u.id)) console.log(`${login}: has NOT authorized this app`);
      else {
        const scopes = granted.get(u.id)!;
        const missing = needed.filter((n) => !scopes.includes(n));
        console.log(`${login}: ${scopes.join(', ') || '(no scopes)'}${missing.length ? `   <-- missing for the bot: ${missing.join(', ')}` : '   (ok for the bot)'}`);
      }
    }
    return;
  }

  if (args[0] === '--delete') {
    const target = args[1];
    const ids = target === 'all' ? (await listAll(token, clientId)).map((s) => s.id as string) : [target];
    for (const id of ids) {
      await helix(`/eventsub/subscriptions?id=${encodeURIComponent(id)}`, token, clientId, { method: 'DELETE' });
      console.log(`deleted ${id}`);
    }
    return;
  }

  const callback = await get('TWITCH_CALLBACK_URL', 'Callback URL (https://<your-worker-domain>/twitch/eventsub)', callbackUrl);
  const secret = await get('TWITCH_EVENTSUB_SECRET', 'EventSub secret (10-100 chars, must match the Worker secret)', eventSubSecret);
  let botId = await get('TWITCH_BOT_USER_ID', 'Bot account (login name or numeric user id)');
  if (!/^\d+$/.test(botId)) {
    const bot = ((await helix(`/users?login=${encodeURIComponent(botId.replace(/^@/, '').toLowerCase())}`, token, clientId)).data as { id: string }[])[0];
    if (!bot) throw new Error(`bot account not found: ${botId}`);
    botId = bot.id;
  }

  const q = channels.map((c) => `login=${encodeURIComponent(c)}`).join('&');
  const users = (await helix(`/users?${q}`, token, clientId)).data as { id: string; login: string }[];
  for (const login of channels) {
    const u = users.find((x) => x.login === login);
    if (!u) {
      console.error(`channel not found: ${login}`);
      continue;
    }
    try {
      const r = await helix('/eventsub/subscriptions', token, clientId, {
        method: 'POST',
        body: JSON.stringify({
          type: 'channel.chat.message',
          version: '1',
          condition: { broadcaster_user_id: u.id, user_id: botId },
          transport: { method: 'webhook', callback, secret },
        }),
      });
      console.log(`created ${r.data[0].id} for ${login} (${r.data[0].status})`);
    } catch (e) {
      console.error(`failed for ${login}: ${(e as Error).message}`);
    }
  }
}

main().catch((e) => {
  console.error(e.message ?? e);
  process.exit(1);
});
export {};
