import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const health = new Hono<AppEnv>();

health.get('/', (c) => c.json({ status: 'Healthy' }));
health.get('/live', (c) => c.json({ status: 'Healthy' }));

// Readiness: the database is the one dependency the API cannot work without.
health.get('/ready', async (c) => {
  try {
    await c.env.DB.prepare('SELECT 1').first();
    return c.json({ status: 'Healthy', checks: { database: 'Healthy' } });
  } catch {
    return c.json({ status: 'Unhealthy', checks: { database: 'Unhealthy' } }, 503);
  }
});
