export const ACTIVE_WINDOW_MINUTES = 60;
export const NO_CHATTERS_MESSAGE = 'No active consenting users found.... Sad Panda.';

const lc = (s: string) => s.toLowerCase();

/** Uniform pick over ALL items (original used Next(0, Count-1): excluded the last, threw on empty). */
export function pickRandom<T>(items: readonly T[], rand: () => number = Math.random): T | undefined {
  if (items.length === 0) return undefined;
  const i = Math.min(items.length - 1, Math.floor(rand() * items.length));
  return items[i];
}

/**
 * Pure candidate selection: distinct active chatters minus disconsenters and the origin user
 * (case-insensitive, matching the original MySQL collation). Returns null when nobody qualifies.
 */
export function pickConsentingChatter(
  activeChatters: readonly string[],
  disconsenters: readonly string[],
  origUser: string,
  rand: () => number = Math.random,
): string | null {
  const excluded = new Set(disconsenters.map(lc));
  excluded.add(lc(origUser));
  const seen = new Set<string>();
  const candidates: string[] = [];
  for (const name of activeChatters) {
    const k = lc(name);
    if (excluded.has(k) || seen.has(k)) continue;
    seen.add(k);
    candidates.push(name);
  }
  return pickRandom(candidates, rand) ?? null;
}

/** Random consenting chatter seen in the last 60 minutes in `channel`, or null if none. */
export async function getRandomConsentingChatter(
  db: D1Database,
  channel: string,
  origUser: string,
  rand: () => number = Math.random,
  now: number = Date.now(),
): Promise<string | null> {
  // last_seen is ISO-8601 UTC text, so lexicographic comparison is chronological.
  const cutoff = new Date(now - ACTIVE_WINDOW_MINUTES * 60_000).toISOString();
  const [chatters, dis] = await db.batch<{ v: string | null }>([
    db
      .prepare('SELECT DISTINCT username AS v FROM active_chatters WHERE channel = ?1 AND last_seen >= ?2')
      .bind(channel, cutoff),
    db.prepare('SELECT name AS v FROM disconsenters WHERE name IS NOT NULL'),
  ]);
  const names = (r: D1Result<{ v: string | null }>) =>
    r.results.map((x) => x.v).filter((v): v is string => v != null);
  return pickConsentingChatter(names(chatters), names(dis), origUser, rand);
}

export async function isDisconsenter(db: D1Database, name: string): Promise<boolean> {
  const row = await db
    .prepare('SELECT 1 AS x FROM disconsenters WHERE name = ?1 COLLATE NOCASE LIMIT 1')
    .bind(name)
    .first();
  return row != null;
}

/**
 * Toggle consent with plain statements (no read-then-write): DELETE first; if nothing was deleted,
 * insert only if still absent (single atomic INSERT ... WHERE NOT EXISTS).
 * Returns the original's message strings.
 */
export async function toggleConsent(db: D1Database, user: string): Promise<string> {
  const del = await db.prepare('DELETE FROM disconsenters WHERE name = ?1 COLLATE NOCASE').bind(user).run();
  if ((del.meta.changes ?? 0) > 0) {
    return `${user} has registered that they DO consent.`;
  }
  await db
    .prepare(
      `INSERT INTO disconsenters (name)
       SELECT ?1 WHERE NOT EXISTS (SELECT 1 FROM disconsenters WHERE name = ?1 COLLATE NOCASE)`,
    )
    .bind(user)
    .run();
  return `${user} has registered they do not consent.`;
}
