import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const cat = new Hono<AppEnv>();

const ENDPOINT = 'https://catfact.ninja/fact';

cat.get('/', async (c) => {
  try {
    const resp = await fetch(ENDPOINT, {
      headers: { Accept: 'application/json', 'User-Agent': 'MiscTwitchChat-Worker' },
    });
    const body = (await resp.json()) as { fact: string };
    return c.text(body.fact);
  } catch {
    return c.text('Internal Server Error', 500);
  }
});
