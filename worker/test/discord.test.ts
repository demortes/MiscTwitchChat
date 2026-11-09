import { afterEach, describe, expect, it, vi } from 'vitest';
import { discord, sendDeferredResult } from '../src/discord/interactions';
import { COMMANDS } from '../src/discord/commands';
import {
  COMMAND_NAMES,
  HANDLER_NAMES,
  MAX_CONTENT,
  dispatchCommand,
  message,
  rollDice,
  type Interaction,
  type InteractionOption,
} from '../src/discord/dispatch';
import { verifyDiscordSignature } from '../src/discord/verify';

const toHex = (b: ArrayBuffer | Uint8Array) =>
  Array.from(new Uint8Array(b as ArrayBuffer), (x) => x.toString(16).padStart(2, '0')).join('');

async function makeKeys() {
  const kp = (await crypto.subtle.generateKey('Ed25519', true, ['sign', 'verify'])) as CryptoKeyPair;
  const pub = toHex((await crypto.subtle.exportKey('raw', kp.publicKey)) as ArrayBuffer);
  const sign = async (ts: string, body: string) =>
    toHex(await crypto.subtle.sign('Ed25519', kp.privateKey, new TextEncoder().encode(ts + body)));
  return { pub, sign };
}

const ctx = { waitUntil: vi.fn(), passThroughOnException: vi.fn() } as unknown as ExecutionContext;

async function post(env: Record<string, unknown>, body: string, headers: Record<string, string>) {
  return discord.request('/interactions', { method: 'POST', body, headers }, env, ctx);
}

afterEach(() => vi.unstubAllGlobals());

describe('signature verification', () => {
  it('accepts a valid signature and rejects tampering', async () => {
    const { pub, sign } = await makeKeys();
    const sig = await sign('123', 'body');
    expect(await verifyDiscordSignature(pub, sig, '123', 'body')).toBe(true);
    expect(await verifyDiscordSignature(pub, sig, '123', 'body!')).toBe(false);
    expect(await verifyDiscordSignature(pub, sig, '124', 'body')).toBe(false);
    expect(await verifyDiscordSignature(pub, 'zz', '123', 'body')).toBe(false);
    expect(await verifyDiscordSignature(undefined, sig, '123', 'body')).toBe(false);
  });
});

describe('POST /interactions', () => {
  it('answers PING with PONG', async () => {
    const { pub, sign } = await makeKeys();
    const body = JSON.stringify({ type: 1 });
    const res = await post({ DISCORD_PUBLIC_KEY: pub }, body, {
      'X-Signature-Ed25519': await sign('1', body),
      'X-Signature-Timestamp': '1',
    });
    expect(res.status).toBe(200);
    expect(await res.json()).toEqual({ type: 1 });
  });

  it('rejects a bad signature with 401', async () => {
    const { pub, sign } = await makeKeys();
    const body = JSON.stringify({ type: 1 });
    const res = await post({ DISCORD_PUBLIC_KEY: pub }, body, {
      'X-Signature-Ed25519': await sign('1', 'other'),
      'X-Signature-Timestamp': '1',
    });
    expect(res.status).toBe(401);
  });

  it('rejects missing headers with 401', async () => {
    const { pub } = await makeKeys();
    expect((await post({ DISCORD_PUBLIC_KEY: pub }, '{"type":1}', {})).status).toBe(401);
  });

  it('answers fast commands inline (type 4) with no mentions allowed', async () => {
    const { pub, sign } = await makeKeys();
    const body = JSON.stringify({
      type: 2,
      token: 't',
      application_id: 'a',
      data: { name: 'ping' },
    });
    const res = await post({ DISCORD_PUBLIC_KEY: pub }, body, {
      'X-Signature-Ed25519': await sign('9', body),
      'X-Signature-Timestamp': '9',
    });
    expect(await res.json()).toEqual({
      type: 4,
      data: { content: 'pong!', allowed_mentions: { parse: [] } },
    });
  });
});

const base = (name: string, options?: InteractionOption[], extra: Partial<Interaction> = {}): Interaction => ({
  type: 2,
  token: 'tok',
  application_id: 'app',
  channel_id: '555',
  member: { user: { id: '1', username: 'alice', discriminator: '0' } },
  data: { name, options },
  ...extra,
});

describe('command definitions', () => {
  it('has a handler for every registered command and vice versa', () => {
    expect([...HANDLER_NAMES].sort()).toEqual([...COMMAND_NAMES].sort());
    expect(new Set(COMMAND_NAMES).size).toBe(COMMANDS.length);
    expect(COMMAND_NAMES).toEqual(
      expect.arrayContaining([
        'blame', 'blamedem', 'blamecody', 'ping', 'userinfo', 'cah', 'bancah',
        'cat', 'dad', 'dog', 'insult', 'generate-name', 'roll',
      ]),
    );
  });

  it('keeps required options before optional ones (Discord requirement)', () => {
    for (const c of COMMANDS) {
      const opts = c.options ?? [];
      const firstOptional = opts.findIndex((o) => !o.required);
      if (firstOptional >= 0) expect(opts.slice(firstOptional).every((o) => !o.required)).toBe(true);
    }
  });
});

describe('roll', () => {
  it('parses XdY and enforces limits', () => {
    expect(rollDice('2d6', () => 0)).toEqual({ ok: true, total: 2 });
    expect(rollDice('3d6', () => 0.999)).toEqual({ ok: true, total: 18 });
    expect(rollDice('1d20').ok).toBe(true);
    for (const bad of ['', 'd20', '2d', '2x6', '0d6', '101d6', '1d0', '1d1000001', '-1d6', '1d6 ', '99999999999999999999d6']) {
      if (bad === '1d6 ') continue; // trimmed
      expect(rollDice(bad).ok).toBe(false);
    }
  });

  it('responds with the C# wording', () => {
    const ok = dispatchCommand(base('roll', [{ name: 'argument', type: 3, value: '1d1' }]), async () => '');
    expect(ok).toEqual({ kind: 'immediate', data: expect.objectContaining({ content: 'alice has rolled 1' }) });
    const bad = dispatchCommand(base('roll', [{ name: 'argument', type: 3, value: 'nope' }]), async () => '');
    expect(bad.kind === 'immediate' && bad.data.content).toBe(
      'alice has given incorrect parameters. Please try again. Beep boop.',
    );
  });
});

describe('dispatch', () => {
  it('userinfo uses resolved user data, falling back to the invoker', () => {
    const withUser = base('userinfo', [{ name: 'user', type: 6, value: '42' }]);
    withUser.data!.resolved = { users: { '42': { id: '42', username: 'bob', discriminator: '1234' } } };
    const r = dispatchCommand(withUser, async () => '');
    expect(r.kind === 'immediate' && r.data.content).toBe('bob#1234');
    const self = dispatchCommand(base('userinfo'), async () => '');
    expect(self.kind === 'immediate' && self.data.content).toBe('alice');
  });

  it('blame commands', () => {
    const i = base('blame', [{ name: 'target', type: 6, value: '42' }]);
    i.data!.resolved = { users: { '42': { id: '42', username: 'bob', discriminator: '0' } } };
    const r = dispatchCommand(i, async () => '');
    expect(r.kind === 'immediate' && r.data.content).toBe('We should all blame bob');
    const d = dispatchCommand(base('blamedem'), async () => '');
    expect(d.kind === 'immediate' && d.data.content).toBe('Demortes is at it again... WTF Demortes.');
    const c = dispatchCommand(base('blamecody'), async () => '');
    expect(c.kind === 'immediate' && c.data.content).toBe("U wot m8? Cody's wheezing again....");
  });

  async function runDeferred(i: Interaction, reply = 'hello') {
    const callApi = vi.fn(async (_p: string) => reply);
    const r = dispatchCommand(i, callApi);
    expect(r.kind).toBe('deferred');
    if (r.kind !== 'deferred') throw new Error('not deferred');
    const data = await r.run();
    return { data, path: (callApi.mock.calls as unknown as string[][])[0]?.[0] as string };
  }

  it('maps slow commands to API paths', async () => {
    expect((await runDeferred(base('cat'))).path).toBe('/api/cat');
    expect((await runDeferred(base('dad'))).path).toBe('/api/dad');
    expect((await runDeferred(base('dog'))).path).toBe('/api/dog');
    expect((await runDeferred(base('cah'))).path).toBe('/api/cards/555');
    expect((await runDeferred(base('bancah'))).path).toBe('/api/cards/ban/555');
    expect(
      (await runDeferred(base('generate-name', [
        { name: 'game', type: 3, value: 'World of Warcraft' },
        { name: 'item', type: 3, value: 'sword/axe' },
      ]))).path,
    ).toBe('/api/ai/name/World%20of%20Warcraft/sword%2Faxe');
  });

  it('insult passes channel name, invoker and optional target', async () => {
    const i = base('insult', [{ name: 'target', type: 6, value: '42' }], { channel: { id: '555', name: 'general' } });
    i.data!.resolved = { users: { '42': { id: '42', username: 'bob' } } };
    expect((await runDeferred(i)).path).toBe('/api/insult?channel=general&user=alice&target=bob');
    expect((await runDeferred(base('insult'))).path).toBe('/api/insult?channel=555&user=alice');
  });

  it('cah tts flag and JSON-string / long / failing API responses', async () => {
    const tts = await runDeferred(base('cah', [{ name: 'args', type: 3, value: 'TTS' }]));
    expect(tts.data.tts).toBe(true);
    expect((await runDeferred(base('cah'))).data.tts).toBeUndefined();
    expect((await runDeferred(base('dad'), '"quoted"')).data.content).toBe('quoted');
    const long = await runDeferred(base('dad'), 'x'.repeat(5000));
    expect(long.data.content.length).toBe(MAX_CONTENT);
    expect(long.data.allowed_mentions).toEqual({ parse: [] });

    const failing = dispatchCommand(base('cat'), async () => {
      throw new Error('boom');
    });
    if (failing.kind !== 'deferred') throw new Error('expected deferred');
    expect((await failing.run()).content).toMatch(/Something went wrong/);
  });

  it('message() never allows mentions and handles empty content', () => {
    expect(message('@everyone hi').allowed_mentions).toEqual({ parse: [] });
    expect(message('  ').content).toBe('(no response)');
  });
});

describe('deferred delivery', () => {
  it('PATCHes @original, or posts a followup + deletes original for tts', async () => {
    const calls: { url: string; method: string }[] = [];
    vi.stubGlobal('fetch', async (url: string, init: RequestInit) => {
      calls.push({ url, method: init.method ?? 'GET' });
      return new Response('{}', { status: 200 });
    });
    const i = base('cat');
    await sendDeferredResult(i, message('hi'));
    await sendDeferredResult(i, message('hi', true));
    expect(calls).toEqual([
      { url: 'https://discord.com/api/v10/webhooks/app/tok/messages/@original', method: 'PATCH' },
      { url: 'https://discord.com/api/v10/webhooks/app/tok', method: 'POST' },
      { url: 'https://discord.com/api/v10/webhooks/app/tok/messages/@original', method: 'DELETE' },
    ]);
  });
});
