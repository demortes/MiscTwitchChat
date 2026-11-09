import { beforeEach, describe, expect, it } from 'vitest';
import { Hono } from 'hono';
import { computeSignature, isTimestampFresh, resetSeen, verifySignature } from '../src/lib/eventsub';
import { parseChatMessageEvent, twitch } from '../src/webhooks/twitch';
import type { AppEnv } from '../src/env';

const SECRET = 'test-secret-1234567890';

async function hmacHex(secret: string, msg: string): Promise<string> {
  const e = new TextEncoder();
  const key = await crypto.subtle.importKey('raw', e.encode(secret), { name: 'HMAC', hash: 'SHA-256' }, false, ['sign']);
  const sig = new Uint8Array(await crypto.subtle.sign('HMAC', key, e.encode(msg)));
  return [...sig].map((b) => b.toString(16).padStart(2, '0')).join('');
}

const chatPayload = () => ({
  subscription: { type: 'channel.chat.message', version: '1' },
  event: { broadcaster_user_login: 'SomeStreamer', chatter_user_login: 'Viewer_1' },
});

function makeApp(runs: unknown[][]) {
  const db = {
    prepare: (sql: string) => ({
      bind: (...args: unknown[]) => ({
        run: async () => {
          runs.push([sql, ...args]);
          return {};
        },
      }),
    }),
  };
  const app = new Hono<AppEnv>();
  app.route('/twitch', twitch);
  return (req: Request) => app.fetch(req, { DB: db, TWITCH_EVENTSUB_SECRET: SECRET } as any);
}

async function signedRequest(type: string, body: unknown, opts: { ts?: string; id?: string; sig?: string } = {}) {
  const raw = JSON.stringify(body);
  const id = opts.id ?? crypto.randomUUID();
  const ts = opts.ts ?? new Date().toISOString();
  const sig = opts.sig ?? 'sha256=' + (await hmacHex(SECRET, id + ts + raw));
  return new Request('http://x/twitch/eventsub', {
    method: 'POST',
    body: raw,
    headers: {
      'Twitch-Eventsub-Message-Id': id,
      'Twitch-Eventsub-Message-Timestamp': ts,
      'Twitch-Eventsub-Message-Signature': sig,
      'Twitch-Eventsub-Message-Type': type,
    },
  });
}

beforeEach(() => resetSeen());

describe('signature', () => {
  it('matches an independently computed HMAC and rejects tampering', async () => {
    const id = 'abc-123';
    const ts = '2026-01-01T00:00:00Z';
    const body = '{"a":1}';
    const expected = 'sha256=' + (await hmacHex(SECRET, id + ts + body));
    expect(await computeSignature(SECRET, id, ts, body)).toBe(expected);
    expect(await verifySignature(SECRET, id, ts, expected, body)).toBe(true);
    expect(await verifySignature(SECRET, id, ts, expected, body + ' ')).toBe(false);
    expect(await verifySignature('other', id, ts, expected, body)).toBe(false);
    expect(await verifySignature(SECRET, id, ts, undefined, body)).toBe(false);
  });
});

describe('timestamp', () => {
  it('accepts fresh and rejects older than 10 minutes', () => {
    const now = Date.parse('2026-01-01T00:20:00Z');
    expect(isTimestampFresh('2026-01-01T00:15:00Z', now)).toBe(true);
    expect(isTimestampFresh('2026-01-01T00:09:00Z', now)).toBe(false);
    expect(isTimestampFresh('garbage', now)).toBe(false);
  });
});

describe('parseChatMessageEvent', () => {
  it('maps logins to lowercase channel/username', () => {
    expect(parseChatMessageEvent(chatPayload())).toEqual({ channel: 'somestreamer', username: 'viewer_1' });
  });
  it('ignores other types and malformed payloads', () => {
    expect(parseChatMessageEvent({ subscription: { type: 'stream.online' }, event: {} })).toBeNull();
    expect(parseChatMessageEvent(null)).toBeNull();
    expect(parseChatMessageEvent({ subscription: { type: 'channel.chat.message' }, event: {} })).toBeNull();
  });
});

describe('POST /twitch/eventsub', () => {
  it('echoes the challenge as text/plain', async () => {
    const f = makeApp([]);
    const res = await f(await signedRequest('webhook_callback_verification', { challenge: 'pogchamp-kappa' }));
    expect(res.status).toBe(200);
    expect(res.headers.get('content-type')).toContain('text/plain');
    expect(await res.text()).toBe('pogchamp-kappa');
  });

  it('403 on bad signature and stale timestamp', async () => {
    const runs: unknown[][] = [];
    const f = makeApp(runs);
    expect((await f(await signedRequest('notification', chatPayload(), { sig: 'sha256=00' }))).status).toBe(403);
    const old = new Date(Date.now() - 11 * 60_000).toISOString();
    expect((await f(await signedRequest('notification', chatPayload(), { ts: old }))).status).toBe(403);
    expect(runs).toHaveLength(0);
  });

  it('upserts on notification with bound params, dedupes by message id', async () => {
    const runs: unknown[][] = [];
    const f = makeApp(runs);
    expect((await f(await signedRequest('notification', chatPayload(), { id: 'm1' }))).status).toBe(204);
    expect(runs).toHaveLength(1);
    expect(runs[0][0]).toContain('ON CONFLICT(channel, username)');
    expect(runs[0][1]).toBe('viewer_1');
    expect(runs[0][2]).toBe('somestreamer');
    expect((await f(await signedRequest('notification', chatPayload(), { id: 'm1' }))).status).toBe(204);
    expect(runs).toHaveLength(1);
  });

  it('204 on revocation without writing', async () => {
    const runs: unknown[][] = [];
    const res = await makeApp(runs)(
      await signedRequest('revocation', { subscription: { type: 'channel.chat.message', status: 'authorization_revoked' } }),
    );
    expect(res.status).toBe(204);
    expect(runs).toHaveLength(0);
  });
});
