import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const dog = new Hono<AppEnv>();

const ENDPOINT = 'https://dogapi.dog/api/v2/facts?number=1';

dog.get('/', async (c) => {
  try {
    const resp = await fetch(ENDPOINT, {
      headers: { Accept: 'application/json', 'User-Agent': 'MiscTwitchChat-Worker' },
    });
    const body = (await resp.json()) as { data: { attributes: { body: string } }[] };
    return c.text(body.data[0].attributes.body);
  } catch {
    return c.text('Internal Server Error', 500);
  }
});
