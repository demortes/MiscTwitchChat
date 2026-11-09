import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const insult = new Hono<AppEnv>();

const BASE = 'https://insult.mattbas.org/api/insult.txt';

// `channel` and `user` query params are accepted but unused (as in the original API).
insult.get('/', async (c) => {
  const target = c.req.query('target');
  let url = BASE;
  if (target && target.trim() !== '') {
    url += `?who=${encodeURIComponent(target)}`;
  }
  try {
    const resp = await fetch(url, { headers: { 'User-Agent': 'MiscTwitchChat-Worker' } });
    if (!resp.ok) return c.text('Internal Server Error', 500);
    return c.text(await resp.text());
  } catch {
    return c.text('Internal Server Error', 500);
  }
});
