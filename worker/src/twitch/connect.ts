import { Hono } from 'hono';
import type { Context } from 'hono';
import { deleteCookie, getCookie, setCookie } from 'hono/cookie';
import type { AppEnv } from '../env';

// Self-service "add the bot to my channel": the broadcaster authorizes this app with `channel:bot`,
// then we create the channel.chat.message EventSub webhook subscription for their channel.
//
//   GET /twitch/connect   -> redirects to Twitch's authorize page
//   GET /twitch/callback  -> Twitch redirects back here with ?code=...&state=...
//
// The broadcaster's token is only used to learn who they are and is then discarded; the subscription
// is created with the app's own token, and Twitch checks the broadcaster's `channel:bot` grant itself.
// The bot account must have authorized the app once (user:read:chat + user:bot); see docs/twitch-eventsub.md.

export const connect = new Hono<AppEnv>();

const STATE_COOKIE = 'twitch_connect_state';
const TOKEN_URL = 'https://id.twitch.tv/oauth2/token';
const HELIX = 'https://api.twitch.tv/helix';

type Env = AppEnv['Bindings'];

function configured(env: Env): boolean {
  return Boolean(env.TWITCH_CLIENT_ID && env.TWITCH_CLIENT_SECRET && env.TWITCH_BOT_USER_ID && env.TWITCH_EVENTSUB_SECRET);
}

const esc = (s: string) =>
  s.replace(/[&<>"']/g, (ch) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[ch]!);

function page(c: Context<AppEnv>, status: 200 | 400 | 404 | 502, title: string, bodyHtml: string) {
  return c.html(
    `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>${esc(title)} - MiscTwitchChat</title>
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-1BmE4kWBq78iYhFldvKuhfTAU6auU8tT94WrHftjDbrCEXSU1oBoqyl2QvZ6jIW3" crossorigin="anonymous">
<link rel="stylesheet" href="/css/custom.css" />
</head>
<body class="h-100 d-flex flex-column">
<main class="flex-shrink-0"><div class="container text-center mt-5">
<h1>${esc(title)}</h1>
${bodyHtml}
<p class="mt-4"><a href="/">Back to the home page</a></p>
</div></main>
</body>
</html>`,
    status,
  );
}

connect.get('/connect', (c) => {
  if (!configured(c.env)) return c.text('Twitch connect is not configured on this instance.', 404);

  const state = crypto.randomUUID();
  setCookie(c, STATE_COOKIE, state, {
    httpOnly: true,
    secure: new URL(c.req.url).protocol === 'https:',
    sameSite: 'Lax',
    path: '/twitch',
    maxAge: 600,
  });

  const params = new URLSearchParams({
    client_id: c.env.TWITCH_CLIENT_ID,
    redirect_uri: new URL('/twitch/callback', c.req.url).toString(),
    response_type: 'code',
    scope: 'channel:bot',
    state,
  });
  return c.redirect(`https://id.twitch.tv/oauth2/authorize?${params}`, 302);
});

async function tokenRequest(env: Env, extra: Record<string, string>): Promise<Response> {
  return fetch(TOKEN_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({ client_id: env.TWITCH_CLIENT_ID, client_secret: env.TWITCH_CLIENT_SECRET, ...extra }),
  });
}

connect.get('/callback', async (c) => {
  if (!configured(c.env)) return c.text('Twitch connect is not configured on this instance.', 404);

  const expected = getCookie(c, STATE_COOKIE);
  deleteCookie(c, STATE_COOKIE, { path: '/twitch' });

  if (c.req.query('error')) {
    return page(c, 400, 'Not connected', '<p>The authorization was cancelled, so nothing was changed.</p><p><a href="/twitch/connect">Try again</a></p>');
  }
  const code = c.req.query('code');
  const state = c.req.query('state');
  if (!code || !state || !expected || state !== expected) {
    return page(c, 400, 'Not connected', '<p>That link has expired or did not start here. Please start again.</p><p><a href="/twitch/connect">Add the bot to my channel</a></p>');
  }

  try {
    // 1. Exchange the code, and check the broadcaster really granted channel:bot.
    const userTokenRes = await tokenRequest(c.env, {
      code,
      grant_type: 'authorization_code',
      redirect_uri: new URL('/twitch/callback', c.req.url).toString(),
    });
    if (!userTokenRes.ok) {
      console.error('twitch connect: code exchange failed', userTokenRes.status);
      return page(c, 502, 'Not connected', '<p>Twitch did not accept the authorization. Please try again.</p>');
    }
    const userToken = (await userTokenRes.json()) as { access_token: string; scope?: string[] };
    if (!userToken.scope?.includes('channel:bot')) {
      return page(c, 400, 'Not connected', '<p>The <code>channel:bot</code> permission was not granted, so the bot cannot read your chat.</p><p><a href="/twitch/connect">Try again</a></p>');
    }

    // 2. Who authorized us?
    const meRes = await fetch(`${HELIX}/users`, {
      headers: { Authorization: `Bearer ${userToken.access_token}`, 'Client-Id': c.env.TWITCH_CLIENT_ID },
    });
    const me = meRes.ok ? ((await meRes.json()) as { data: { id: string; login: string; display_name: string }[] }).data[0] : undefined;
    if (!me) return page(c, 502, 'Not connected', '<p>Could not read your Twitch account. Please try again.</p>');

    // 3. Subscribe with the app's own token. 409 means this channel is already subscribed.
    const appTokenRes = await tokenRequest(c.env, { grant_type: 'client_credentials' });
    if (!appTokenRes.ok) {
      console.error('twitch connect: app token failed', appTokenRes.status);
      return page(c, 502, 'Not connected', '<p>Could not reach Twitch. Please try again later.</p>');
    }
    const appToken = ((await appTokenRes.json()) as { access_token: string }).access_token;

    const subRes = await fetch(`${HELIX}/eventsub/subscriptions`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${appToken}`, 'Client-Id': c.env.TWITCH_CLIENT_ID, 'Content-Type': 'application/json' },
      body: JSON.stringify({
        type: 'channel.chat.message',
        version: '1',
        condition: { broadcaster_user_id: me.id, user_id: c.env.TWITCH_BOT_USER_ID },
        transport: {
          method: 'webhook',
          callback: new URL('/twitch/eventsub', c.req.url).toString(),
          secret: c.env.TWITCH_EVENTSUB_SECRET,
        },
      }),
    });

    const name = esc(me.display_name || me.login);
    if (subRes.status === 409) {
      return page(c, 200, 'Already connected', `<p>The bot is already set up for <strong>${name}</strong>. Nothing to do.</p>`);
    }
    if (!subRes.ok) {
      console.error('twitch connect: subscribe failed', subRes.status, await subRes.text());
      return page(
        c,
        502,
        'Not connected',
        '<p>Twitch would not create the subscription. The bot account may not be set up yet; please let the site owner know.</p>',
      );
    }
    return page(c, 200, 'Connected', `<p>The bot is now set up for <strong>${name}</strong>. Chat activity in your channel will be picked up from here on.</p>`);
  } catch (err) {
    console.error('twitch connect: unexpected error', err instanceof Error ? err.message : err);
    return page(c, 502, 'Not connected', '<p>Something went wrong talking to Twitch. Please try again later.</p>');
  }
});
