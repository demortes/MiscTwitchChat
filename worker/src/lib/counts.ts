/** Atomically increment a command counter (single upsert; safe under concurrency). */
export async function incrementCommandCount(
  db: D1Database,
  channel: string,
  targetUser: string,
  commandUsed: string,
): Promise<void> {
  await db
    .prepare(
      `INSERT INTO command_counts (channel, target_user, command_used, count)
       VALUES (?1, ?2, ?3, 1)
       ON CONFLICT(channel, command_used, target_user) DO UPDATE SET count = count + 1`,
    )
    .bind(channel, targetUser, commandUsed)
    .run();
}

/** Current count, or 0 if no row exists. */
export async function getCommandCount(
  db: D1Database,
  channel: string,
  targetUser: string,
  commandUsed: string,
): Promise<number> {
  const row = await db
    .prepare(
      `SELECT count FROM command_counts
       WHERE channel = ?1 AND target_user = ?2 AND command_used = ?3`,
    )
    .bind(channel, targetUser, commandUsed)
    .first<{ count: number }>();
  return row?.count ?? 0;
}
