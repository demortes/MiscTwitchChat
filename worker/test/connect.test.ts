import { afterEach, describe, expect, it, vi } from 'vitest';
import { connect } from '../src/twitch/connect';

const env = {
  TWITCH_CLIENT_ID: 'client-id',
  TWITCH_CLIENT_SECRET: 'client-secret',
  TWITCH_BOT_USER_ID: '999',
  TWITCH_EVENTSUB_SECRET: 'eventsub-secret-1234',
};

afterEach(() => vi.unstubAllGlobals());

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });

type Handler = (url: string, init?: RequestInit) => Response | undefined;

/** Stubs fetch with a handler; anything unhandled fails the test. Returns the recorded calls. */
function stubFetch(handler: Handler) {
  const calls: { url: string; init?: RequestInit }[] = [];
  vi.stubGlobal('fetch', async (input: string | URL | Request, init?: RequestInit) => {
    const url = String(input);
    calls.push({ url, init });
    const res = handler(url, init);
    if (!res) throw new Error(`unexpected fetch ${url}`);
    return res;
  });
  return calls;
}

// Happy path: broadcaster 123 "SomeStreamer" granted channel:bot.
const twitchOk = (subscribeStatus = 202): Handler => (url, init) => {
  if (url === 'https://id.twitch.tv/oauth2/token') {
    const grant = new URLSearchParams(String(init?.body)).get('grant_type');
    return grant === 'authorization_code'
      ? json({ access_token: 'user-token', scope: ['channel:bot'] })
      : json({ access_token: 'app-token' });
  }
  if (url === 'https://api.twitch.tv/helix/users') return json({ data: [{ id: '123', login: 'somestreamer', display_name: 'SomeStreamer' }] });
  if (url === 'https://api.twitch.tv/helix/eventsub/subscriptions') return json({ data: [{ id: 'sub-1' }] }, subscribeStatus);
  return undefined;
};

const callback = (query: string, cookie = 'twitch_connect_state=abc', e: Record<string, string> = env) =>
  connect.request(`/callback?${query}`, { headers: cookie ? { cookie } : {} }, e as never);

describe('GET /connect', () => {
  it('is a 404 when the instance has no Twitch app configured', async () => {
    expect((await connect.request('/connect', {}, {} as never)).status).toBe(404);
    expect((await connect.request('/connect', {}, { TWITCH_CLIENT_ID: 'x' } as never)).status).toBe(404);
  });

  it('redirects to Twitch asking only for channel:bot, with a state that is also set as a cookie', async () => {
    const res = await connect.request('/connect', {}, env as never);
    expect(res.status).toBe(302);
    const loc = new URL(res.headers.get('location')!);
    expect(loc.origin + loc.pathname).toBe('https://id.twitch.tv/oauth2/authorize');
    expect(loc.searchParams.get('client_id')).toBe('client-id');
    expect(loc.searchParams.get('response_type')).toBe('code');
    expect(loc.searchParams.get('scope')).toBe('channel:bot');
    expect(loc.searchParams.get('redirect_uri')).toBe('http://localhost/twitch/callback');
    const state = loc.searchParams.get('state')!;
    expect(state.length).toBeGreaterThan(20);
    const cookie = res.headers.get('set-cookie')!;
    expect(cookie).toContain(`twitch_connect_state=${state}`);
    expect(cookie).toMatch(/HttpOnly/i);
    expect(cookie).toMatch(/SameSite=Lax/i);
  });

  it('uses a fresh state each time', async () => {
    const a = new URL((await connect.request('/connect', {}, env as never)).headers.get('location')!);
    const b = new URL((await connect.request('/connect', {}, env as never)).headers.get('location')!);
    expect(a.searchParams.get('state')).not.toBe(b.searchParams.get('state'));
  });
});

describe('GET /callback', () => {
  it('rejects a missing or mismatched state without calling Twitch', async () => {
    const calls = stubFetch(() => undefined);
    expect((await callback('code=c&state=other')).status).toBe(400);
    expect((await callback('code=c&state=abc', '')).status).toBe(400);
    expect((await callback('code=c')).status).toBe(400);
    expect(calls).toHaveLength(0);
  });

  it('treats a cancelled authorization as nothing happened', async () => {
    const calls = stubFetch(() => undefined);
    const res = await callback('error=access_denied&state=abc');
    expect(res.status).toBe(400);
    expect(await res.text()).toContain('cancelled');
    expect(calls).toHaveLength(0);
  });

  it("subscribes the authorizing broadcaster's channel using the app token and the bot as the chatting user", async () => {
    const calls = stubFetch(twitchOk());
    const res = await callback('code=thecode&state=abc');
    expect(res.status).toBe(200);
    const html = await res.text();
    expect(html).toContain('Connected');
    expect(html).toContain('SomeStreamer');

    const exchange = calls[0];
    const form = new URLSearchParams(String(exchange.init?.body));
    expect(form.get('code')).toBe('thecode');
    expect(form.get('client_secret')).toBe('client-secret');
    expect(form.get('redirect_uri')).toBe('http://localhost/twitch/callback');

    const sub = calls.find((x) => x.url.endsWith('/eventsub/subscriptions'))!;
    expect((sub.init?.headers as Record<string, string>).Authorization).toBe('Bearer app-token');
    expect(JSON.parse(String(sub.init?.body))).toEqual({
      type: 'channel.chat.message',
      version: '1',
      condition: { broadcaster_user_id: '123', user_id: '999' },
      transport: { method: 'webhook', callback: 'http://localhost/twitch/eventsub', secret: 'eventsub-secret-1234' },
    });
  });

  it('clears the state cookie so the callback cannot be replayed', async () => {
    stubFetch(twitchOk());
    const res = await callback('code=c&state=abc');
    expect(res.headers.get('set-cookie')).toMatch(/twitch_connect_state=;/);
  });

  it('says so when the channel is already subscribed (409)', async () => {
    stubFetch(twitchOk(409));
    const res = await callback('code=c&state=abc');
    expect(res.status).toBe(200);
    expect(await res.text()).toContain('Already connected');
  });

  it('refuses to subscribe if channel:bot was not granted', async () => {
    const calls = stubFetch((url) =>
      url === 'https://id.twitch.tv/oauth2/token' ? json({ access_token: 't', scope: ['user:read:chat'] }) : undefined,
    );
    const res = await callback('code=c&state=abc');
    expect(res.status).toBe(400);
    expect(calls.some((x) => x.url.includes('eventsub'))).toBe(false);
  });

  it('reports a failed code exchange without leaking details', async () => {
    stubFetch(() => json({ message: 'Invalid authorization code' }, 400));
    const res = await callback('code=bad&state=abc');
    expect(res.status).toBe(502);
    expect(await res.text()).not.toContain('Invalid authorization code');
  });

  it('reports a rejected subscription (for example the bot account has not authorized the app) as a failure', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    stubFetch((url, init) =>
      url.endsWith('/eventsub/subscriptions')
        ? json({ message: 'subscription missing proper authorization' }, 403)
        : twitchOk()(url, init),
    );
    const res = await callback('code=c&state=abc');
    expect(res.status).toBe(502);
    expect(await res.text()).not.toContain('missing proper authorization');
  });

  it('escapes the display name in the page', async () => {
    stubFetch((url, init) =>
      url === 'https://api.twitch.tv/helix/users'
        ? json({ data: [{ id: '1', login: 'x', display_name: '<img src=x onerror=alert(1)>' }] })
        : twitchOk()(url, init),
    );
    const html = await (await callback('code=c&state=abc')).text();
    expect(html).not.toContain('<img');
    expect(html).toContain('&lt;img');
  });

  it('is a 404 when the instance has no Twitch app configured', async () => {
    expect((await callback('code=c&state=abc', 'twitch_connect_state=abc', {})).status).toBe(404);
  });
});
