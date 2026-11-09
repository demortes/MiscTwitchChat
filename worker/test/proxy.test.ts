import { afterEach, describe, expect, it, vi } from 'vitest';
import { Hono } from 'hono';
import type { AppEnv } from '../src/env';
import { urban, formatDefinition, DEMORTES_MSG, CHRISSIE_MSG, ERROR_MSG } from '../src/routes/urban';
import { aiName, DEFAULT_MODEL } from '../src/routes/aiName';
import { dictionary, pickMeaning } from '../src/routes/dictionary';

const env = { OPEN_AI_KEY: 'sk-test', OPEN_AI_URL: 'https://ai.example/v1/chat' } as any;

function app() {
  const a = new Hono<AppEnv>();
  a.route('/api/urban', urban);
  a.route('/api/ai/name', aiName);
  a.route('/api/dictionary', dictionary);
  return a;
}

const json = (b: unknown, status = 200) =>
  new Response(JSON.stringify(b), { status, headers: { 'content-type': 'application/json' } });

afterEach(() => vi.unstubAllGlobals());

describe('urban', () => {
  it('truncates at 175 chars with ellipsis and flattens newlines', async () => {
    const def = 'a'.repeat(100) + '\r\n' + 'b'.repeat(200);
    vi.stubGlobal('fetch', vi.fn(async () => json({ list: [{ definition: def, thumbs_up: 1, thumbs_down: 0 }] })));
    const res = await app().request('/api/urban/foo', {}, env);
    const text = await res.text();
    expect(res.headers.get('content-type')).toContain('text/plain');
    expect(text.length).toBe(178);
    expect(text.endsWith('...')).toBe(true);
    expect(text).not.toMatch(/[\r\n]/);
  });

  it('does not add ellipsis at exactly 175 chars', () => {
    expect(formatDefinition('x'.repeat(175))).toBe('x'.repeat(175));
    expect(formatDefinition('x'.repeat(176))).toBe('x'.repeat(175) + '...');
  });

  it('picks highest-voted definition', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        json({
          list: [
            { definition: 'low', thumbs_up: 1, thumbs_down: 5 },
            { definition: 'high', thumbs_up: 10, thumbs_down: 1 },
          ],
        }),
      ),
    );
    expect(await (await app().request('/api/urban/x', {}, env)).text()).toBe('high');
  });

  it('returns special messages case-insensitively without fetching', async () => {
    const f = vi.fn();
    vi.stubGlobal('fetch', f);
    expect(await (await app().request('/api/urban/DeMortes', {}, env)).text()).toBe(DEMORTES_MSG);
    expect(await (await app().request('/api/urban/LITTLECHRISSIE', {}, env)).text()).toBe(CHRISSIE_MSG);
    expect(f).not.toHaveBeenCalled();
  });

  it('returns not-found message', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => json({ list: [] })));
    expect(await (await app().request('/api/urban/zzzz', {}, env)).text()).toBe('404: zzzz not found.');
  });

  it('returns abort message on failure', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => {
        throw new Error('boom');
      }),
    );
    expect(await (await app().request('/api/urban/x', {}, env)).text()).toBe(ERROR_MSG);
  });
});

describe('aiName', () => {
  it('returns Error string with .NET status name on upstream failure', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('nope', { status: 400 })));
    const res = await app().request('/api/ai/name/Zelda/sword', {}, env);
    expect(await res.text()).toBe('Error: BadRequest - nope');
  });

  it('returns the completion content and builds the prompt', async () => {
    const f = vi.fn(async (_u: string, _i: RequestInit) => json({ choices: [{ message: { content: 'Sword Art' } }] }));
    vi.stubGlobal('fetch', f);
    const res = await app().request('/api/ai/name/Zelda/sword', {}, env);
    expect(await res.text()).toBe('Sword Art');
    const body = JSON.parse(f.mock.calls[0][1].body as string);
    expect(body.messages[1].content).toContain('a sword within the game Zelda');
  });

  it('uses the OPEN_AI_MODEL setting, and a default when it is not set or blank', async () => {
    const f = vi.fn(async (_u: string, _i: RequestInit) => json({ choices: [{ message: { content: 'x' } }] }));
    vi.stubGlobal('fetch', f);
    await app().request('/api/ai/name/Zelda/sword', {}, { ...env, OPEN_AI_MODEL: 'some-model' });
    await app().request('/api/ai/name/Zelda/sword', {}, env);
    await app().request('/api/ai/name/Zelda/sword', {}, { ...env, OPEN_AI_MODEL: '  ' });
    const models = f.mock.calls.map((c) => JSON.parse(c[1].body as string).model);
    expect(models).toEqual(['some-model', DEFAULT_MODEL, DEFAULT_MODEL]);
  });

  it('errors when config is missing', async () => {
    const res = await app().request('/api/ai/name/a/b', {}, {} as any);
    expect(res.status).toBe(500);
  });
});

describe('dictionary', () => {
  it('prefers shortdef, falls back to fl', () => {
    expect(pickMeaning([{ shortdef: ['a thing'] }])).toBe('a thing');
    expect(pickMeaning([{ shortdef: [], fl: 'noun', def: [{}] }])).toBe('noun');
    expect(pickMeaning([{ shortdef: [] }])).toBeNull();
  });

  it('passes through upstream error status', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('', { status: 403 })));
    expect((await app().request('/api/dictionary/cat', {}, env)).status).toBe(403);
  });
});
