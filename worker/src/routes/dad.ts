import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const dad = new Hono<AppEnv>();

dad.get('/', async (c) => {
  try {
    const resp = await fetch('https://icanhazdadjoke.com', {
      headers: { Accept: 'text/plain', 'User-Agent': 'MiscTwitchChat-Worker' },
    });
    return c.text(await resp.text());
  } catch {
    return c.text('Internal Server Error', 500);
  }
});
