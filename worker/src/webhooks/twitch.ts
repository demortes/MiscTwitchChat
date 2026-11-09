import { Hono } from 'hono';
import type { AppEnv } from '../env';
import { isTimestampFresh, seenBefore, verifySignature } from '../lib/eventsub';

export const twitch = new Hono<AppEnv>();

export interface ChatterRow {
  channel: string;
  username: string;
}

/**
 * Pure: map a channel.chat.message EventSub payload to an active_chatters key.
 * Uses broadcaster_user_login / chatter_user_login (lowercase, no '#'), matching the
 * IRC-based bot's ChatMessage.Channel / ChatMessage.Username. Returns null if not applicable.
 */
export function parseChatMessageEvent(payload: unknown): ChatterRow | null {
  if (typeof payload !== 'object' || payload === null) return null;
  const p = payload as { subscription?: { type?: unknown }; event?: Record<string, unknown> };
  if (p.subscription?.type !== 'channel.chat.message') return null;
  const ev = p.event;
  if (!ev) return null;
  const channel = ev.broadcaster_user_login;
  const username = ev.chatter_user_login;
  if (typeof channel !== 'string' || typeof username !== 'string' || !channel || !username) return null;
  return { channel: channel.replace(/^#/, '').toLowerCase(), username: username.toLowerCase() };
}

// Writes at most once per 60s per (channel, username) to conserve D1 write budget.
// The WHERE on DO UPDATE makes a skipped update a no-op (no row written).
export const UPSERT_SQL = `INSERT INTO active_chatters (username, channel, last_seen, first_seen)
VALUES (?1, ?2, ?3, ?3)
ON CONFLICT(channel, username) DO UPDATE SET last_seen = excluded.last_seen
WHERE julianday(excluded.last_seen) - julianday(active_chatters.last_seen) > 60 / 86400.0`;

twitch.post('/eventsub', async (c) => {
  const raw = await c.req.text();
  const h = c.req.header.bind(c.req);
  const messageId = h('Twitch-Eventsub-Message-Id');
  const timestamp = h('Twitch-Eventsub-Message-Timestamp');
  const signature = h('Twitch-Eventsub-Message-Signature');
  const type = h('Twitch-Eventsub-Message-Type');

  if (!(await verifySignature(c.env.TWITCH_EVENTSUB_SECRET, messageId, timestamp, signature, raw))) {
    return c.text('invalid signature', 403);
  }
  if (!isTimestampFresh(timestamp)) {
    return c.text('stale timestamp', 403);
  }

  let payload: any;
  try {
    payload = JSON.parse(raw);
  } catch {
    return c.text('bad json', 400);
  }

  switch (type) {
    case 'webhook_callback_verification':
      return c.text(String(payload?.challenge ?? ''), 200);

    case 'notification': {
      if (messageId && seenBefore(messageId)) return c.body(null, 204);
      const row = parseChatMessageEvent(payload);
      if (row) {
        await c.env.DB.prepare(UPSERT_SQL)
          .bind(row.username, row.channel, new Date().toISOString())
          .run();
      }
      return c.body(null, 204);
    }

    case 'revocation':
      console.warn('EventSub revocation', JSON.stringify(payload?.subscription ?? {}));
      return c.body(null, 204);

    default:
      return c.body(null, 204);
  }
});
