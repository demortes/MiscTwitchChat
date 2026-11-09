import { Hono } from 'hono';
import type { AppEnv } from '../env';
import { buildCahCard, type CahData } from '../lib/cah';
import cahJson from '../data/cah_cards.json';

export const cards = new Hono<AppEnv>();

const BAN_SETTING = 'BANCAH';
const cahData = cahJson as unknown as CahData;

const BANNED_MSG =
  'As CAH opens its mouth to impart words of callous wisdom, a magical force press it shut. Someone has been a bad algorithm.';

async function getBanValue(db: D1Database, channel: string): Promise<string | null> {
  const row = await db
    .prepare('SELECT value FROM settings WHERE channel = ?1 AND name = ?2')
    .bind(channel, BAN_SETTING)
    .first<{ value: string | null }>();
  return row ? (row.value ?? '') : null;
}

async function banCah(db: D1Database, channel: string): Promise<string> {
  try {
    const existing = await getBanValue(db, channel);
    const lastBan = existing == null ? NaN : Date.parse(existing);
    // No row, or last ban (+3 min) has passed (unparseable legacy values count as expired).
    if (existing == null || Number.isNaN(lastBan) || lastBan + 3 * 60_000 < Date.now()) {
      const until = new Date(Date.now() + 5 * 60_000).toISOString();
      await db
        .prepare(
          `INSERT INTO settings (channel, name, value) VALUES (?1, ?2, ?3)
           ON CONFLICT(channel, name) DO UPDATE SET value = excluded.value`,
        )
        .bind(channel, BAN_SETTING, until)
        .run();
      return "CAH has been silenced for a while. Let's hope the punishment fit the crime.";
    }
    if (lastBan + 3 * 60_000 > Date.now()) return 'CAH was banned too recently. Give peace a chance.';
    return 'CAH is already banned. Let the punishment lapse first.';
  } catch {
    return 'Something went wrong. Please DM Demortes';
  }
}

cards.get('/', (c) => c.text(buildCahCard(cahData)));

// Registered before '/:channel' so 'ban' is not treated as a channel name.
cards.get('/ban/:channel', async (c) => c.text(await banCah(c.env.DB, c.req.param('channel'))));
cards.delete('/:channel', async (c) => c.text(await banCah(c.env.DB, c.req.param('channel'))));

cards.get('/:channel', async (c) => {
  const channel = c.req.param('channel');
  const ban = await getBanValue(c.env.DB, channel);
  if (ban != null) {
    const until = Date.parse(ban);
    if (!Number.isNaN(until) && until > Date.now()) return c.text(BANNED_MSG);
  }
  return c.text(buildCahCard(cahData));
});
