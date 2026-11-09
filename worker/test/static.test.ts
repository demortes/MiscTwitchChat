import { describe, expect, it } from 'vitest';
import { Hono } from 'hono';
import type { AppEnv } from '../src/env';
import { eightball, RESPONSES } from '../src/routes/eightball';
import { health } from '../src/routes/health';
import { docs } from '../src/routes/docs';
import { openapi } from '../src/openapi';

const app = new Hono<AppEnv>();
app.route('/api/eightball', eightball);
app.route('/health', health);
app.route('/', docs);

const okDb = { prepare: () => ({ first: async () => ({ '1': 1 }) }) } as unknown as D1Database;
const badDb = { prepare: () => ({ first: async () => { throw new Error('down'); } }) } as unknown as D1Database;

describe('eightball', () => {
  it('eightball returns a response from the set as text/plain', async () => {
    for (let i = 0; i < 20; i++) {
      const res = await app.request('/api/eightball');
      expect(res.status).toBe(200);
      expect(res.headers.get('content-type')).toContain('text/plain');
      expect(RESPONSES).toContain(await res.text());
    }
  });
});

describe('health', () => {
  it('GET /health is healthy', async () => {
    const res = await app.request('/health', {}, { DB: okDb });
    expect(res.status).toBe(200);
    expect(await res.json()).toMatchObject({ status: 'Healthy' });
  });

  it('GET /health/ready checks the database', async () => {
    expect((await app.request('/health/ready', {}, { DB: okDb })).status).toBe(200);
    const bad = await app.request('/health/ready', {}, { DB: badDb });
    expect(bad.status).toBe(503);
    expect(await bad.json()).toMatchObject({ status: 'Unhealthy' });
  });
});

describe('docs', () => {
  it('serves the openapi document at both paths and the swagger page', async () => {
    for (const p of ['/openapi.json', '/swagger/v1/swagger.json']) {
      const res = await app.request(p);
      expect(res.status).toBe(200);
      expect((await res.json() as { openapi: string }).openapi).toMatch(/^3\./);
    }
    const page = await app.request('/swagger');
    expect(page.status).toBe(200);
    expect(await page.text()).toMatch(/swagger-ui-dist@\d+\.\d+\.\d+/);
  });

  it('openapi covers every mounted route', () => {
    const expected: Record<string, string[]> = {
      '/api/cat': ['get'],
      '/api/dad': ['get'],
      '/api/dog': ['get'],
      '/api/insult': ['get'],
      '/api/dictionary/{word}': ['get'],
      '/api/urban/{term}': ['get'],
      '/api/tweet/{twitterUsername}': ['get'],
      '/api/ai/name/{game}/{item}': ['get'],
      '/api/hug/{channel}/{origUser}': ['get'],
      '/api/hug/{channel}/{origUser}/consent': ['get'],
      '/api/poke/{channel}/{origUser}': ['get'],
      '/api/poke/{channel}/{origUser}/consent': ['get'],
      '/api/spank/{channel}/{origUser}': ['get'],
      '/api/spank/{channel}/{origUser}/consent': ['get'],
      '/api/gift': ['get'],
      '/api/cards': ['get'],
      '/api/cards/{channel}': ['get', 'delete'],
      '/api/cards/ban/{channel}': ['get'],
      '/api/commandcounts/{channel}/{targetUser}/{commandUsed}': ['get'],
      '/api/eightball': ['get'],
    };
    const paths = openapi.paths as Record<string, Record<string, unknown>>;
    expect(Object.keys(paths).sort()).toEqual(Object.keys(expected).sort());
    for (const [p, methods] of Object.entries(expected)) {
      expect(Object.keys(paths[p]).sort()).toEqual([...methods].sort());
    }
  });
});
