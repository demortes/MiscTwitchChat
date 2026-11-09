// Manage channel.chat.message EventSub webhook subscriptions.
//
// Run with Node >= 22.18 (type stripping is on by default; Node 22.6-22.17 needs
// `--experimental-strip-types`):
//   node scripts/twitch-subscribe.ts <channel...>         create subscriptions
//   node scripts/twitch-subscribe.ts --list               list subscriptions
//   node scripts/twitch-subscribe.ts --delete <id|all>    delete one or all
//
// Env: TWITCH_CLIENT_ID, TWITCH_CLIENT_SECRET, TWITCH_EVENTSUB_SECRET,
//      TWITCH_CALLBACK_URL (e.g. https://<worker>.workers.dev/twitch/eventsub),
//      TWITCH_BOT_USER_ID (numeric id of the bot/user whose chat read permission is used;
//      that user must have authorized the app with user:read:chat -- see docs/twitch-eventsub.md).
// Channel args are logins (e.g. somestreamer); they are resolved to numeric ids via Helix.

declare const process: { argv: string[]; env: Record<string, string | undefined>; exit(code: number): never };

const HELIX = 'https://api.twitch.tv/helix';

function need(name: string): string {
  const v = process.env[name];
  if (!v) {
    console.error(`Missing env var ${name}`);
    process.exit(1);
  }
  return v;
}

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
  const clientId = need('TWITCH_CLIENT_ID');
  const token = await appToken(clientId, need('TWITCH_CLIENT_SECRET'));

  if (args[0] === '--list') {
    for (const s of await listAll(token, clientId)) {
      console.log(`${s.id}  ${s.status}  ${s.type}  ${JSON.stringify(s.condition)}  ${s.transport.callback ?? ''}`);
    }
    return;
  }

  if (args[0] === '--delete') {
    const target = args[1];
    if (!target) throw new Error('--delete requires <id|all>');
    const ids = target === 'all' ? (await listAll(token, clientId)).map((s) => s.id as string) : [target];
    for (const id of ids) {
      await helix(`/eventsub/subscriptions?id=${encodeURIComponent(id)}`, token, clientId, { method: 'DELETE' });
      console.log(`deleted ${id}`);
    }
    return;
  }

  const channels = args.filter((a) => !a.startsWith('--')).map((c) => c.replace(/^#/, '').toLowerCase());
  if (channels.length === 0) throw new Error('Usage: twitch-subscribe.ts <channel...> | --list | --delete <id|all>');
  const callback = need('TWITCH_CALLBACK_URL');
  const secret = need('TWITCH_EVENTSUB_SECRET');
  const botId = need('TWITCH_BOT_USER_ID');

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
