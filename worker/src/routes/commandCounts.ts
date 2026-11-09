import { Hono } from 'hono';
import type { AppEnv } from '../env';
import { getCommandCount } from '../lib/counts';

export const commandCounts = new Hono<AppEnv>();

commandCounts.get('/:channel/:targetUser/:commandUsed', async (c) => {
  const { channel, targetUser, commandUsed } = c.req.param();
  return c.text(String(await getCommandCount(c.env.DB, channel, targetUser, commandUsed)));
});
