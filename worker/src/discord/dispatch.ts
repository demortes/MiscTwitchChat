import { COMMANDS } from './commands';

export const MAX_CONTENT = 2000;
export const MAX_DICE = 100;
export const MAX_SIDES = 1_000_000;

export interface DiscordUser {
  id: string;
  username: string;
  discriminator?: string;
  global_name?: string | null;
}

export interface InteractionOption {
  name: string;
  type: number;
  value?: string | number | boolean;
}

export interface Interaction {
  type: number;
  id?: string;
  token: string;
  application_id: string;
  channel_id?: string;
  channel?: { id: string; name?: string };
  member?: { user: DiscordUser };
  user?: DiscordUser;
  data?: {
    name: string;
    options?: InteractionOption[];
    resolved?: { users?: Record<string, DiscordUser> };
  };
}

export interface MessageData {
  content: string;
  tts?: boolean;
  allowed_mentions: { parse: never[] };
}

export type CommandResult =
  | { kind: 'immediate'; data: MessageData }
  | { kind: 'deferred'; run: () => Promise<MessageData> };

/** GETs an internal API path (e.g. "/api/cat") and returns the response text. Throws on non-2xx. */
export type CallApi = (path: string) => Promise<string>;

/** Never allow any mention to ping (no @everyone/@here/roles/users). */
export function message(content: string, tts?: boolean): MessageData {
  let text = content.trim() === '' ? '(no response)' : content;
  if (text.length > MAX_CONTENT) text = text.slice(0, MAX_CONTENT - 1) + '…';
  const data: MessageData = { content: text, allowed_mentions: { parse: [] } };
  if (tts) data.tts = true;
  return data;
}

/** Discord.Net IUser.ToString(): "name#1234", or just "name" for migrated usernames. */
export function userString(u: DiscordUser): string {
  return u.discriminator && u.discriminator !== '0' ? `${u.username}#${u.discriminator}` : u.username;
}

function invoker(i: Interaction): DiscordUser {
  return i.member?.user ?? i.user ?? { id: '0', username: 'Unknown' };
}

function stringOpt(i: Interaction, name: string): string | undefined {
  const v = i.data?.options?.find((o) => o.name === name)?.value;
  return v === undefined || v === null ? undefined : String(v);
}

function userOpt(i: Interaction, name: string): DiscordUser | undefined {
  const id = stringOpt(i, name);
  if (!id) return undefined;
  return i.data?.resolved?.users?.[id] ?? { id, username: id };
}

/** ASP.NET returns string results as text/plain; tolerate JSON-encoded strings too. */
export function cleanApiText(raw: string): string {
  const t = raw.trim();
  if (t.length >= 2 && t.startsWith('"') && t.endsWith('"')) {
    try {
      const parsed: unknown = JSON.parse(t);
      if (typeof parsed === 'string') return parsed;
    } catch {
      /* fall through */
    }
  }
  return raw;
}

export type RollResult = { ok: true; total: number } | { ok: false };

/** Parses "XdY" (like the C# regex ^\d+d\d+$) and rolls. Enforces 1..MAX_DICE dice and 1..MAX_SIDES sides. */
export function rollDice(argument: string, rand: () => number = Math.random): RollResult {
  const m = /^(\d+)d(\d+)$/.exec(argument.trim());
  if (!m) return { ok: false };
  const dice = Number(m[1]);
  const sides = Number(m[2]);
  if (!Number.isSafeInteger(dice) || !Number.isSafeInteger(sides)) return { ok: false };
  if (dice < 1 || dice > MAX_DICE || sides < 1 || sides > MAX_SIDES) return { ok: false };
  let total = 0;
  for (let n = 0; n < dice; n++) total += 1 + Math.floor(rand() * sides);
  return { ok: true, total };
}

const enc = encodeURIComponent;

/** A deferred command: runs `path` against the API, then formats the text. */
function viaApi(callApi: CallApi, path: string, tts = false): CommandResult {
  return {
    kind: 'deferred',
    run: async () => {
      try {
        return message(cleanApiText(await callApi(path)), tts);
      } catch (err) {
        console.error('discord command API call failed', path, err);
        return message('Something went wrong. Please DM Demortes');
      }
    },
  };
}

type Handler = (i: Interaction, callApi: CallApi) => CommandResult;
const now = (data: MessageData): CommandResult => ({ kind: 'immediate', data });

export const HANDLERS: Record<string, Handler> = {
  blame: (i) => {
    const target = userOpt(i, 'target');
    return now(message(`We should all blame ${target ? userString(target) : 'nobody'}`));
  },
  blamedem: () => now(message('Demortes is at it again... WTF Demortes.')),
  blamecody: () => now(message("U wot m8? Cody's wheezing again....")),
  ping: () => now(message('pong!')),
  userinfo: (i) => now(message(userString(userOpt(i, 'user') ?? invoker(i)))),
  cah: (i, callApi) => {
    const args = stringOpt(i, 'args');
    const tts = !!args && args.trim() !== '' && args.toLowerCase() === 'tts';
    return viaApi(callApi, `/api/cards/${enc(i.channel_id ?? i.channel?.id ?? '')}`, tts);
  },
  bancah: (i, callApi) => viaApi(callApi, `/api/cards/ban/${enc(i.channel_id ?? i.channel?.id ?? '')}`),
  cat: (_i, callApi) => viaApi(callApi, '/api/cat'),
  dad: (_i, callApi) => viaApi(callApi, '/api/dad'),
  dog: (_i, callApi) => viaApi(callApi, '/api/dog'),
  insult: (i, callApi) => {
    const params = new URLSearchParams();
    params.set('channel', i.channel?.name ?? i.channel_id ?? '');
    params.set('user', invoker(i).username);
    const target = userOpt(i, 'target');
    if (target) params.set('target', target.username);
    return viaApi(callApi, `/api/insult?${params.toString()}`);
  },
  'generate-name': (i, callApi) =>
    viaApi(callApi, `/api/ai/name/${enc(stringOpt(i, 'game') ?? '')}/${enc(stringOpt(i, 'item') ?? '')}`),
  roll: (i) => {
    const name = invoker(i).username;
    const r = rollDice(stringOpt(i, 'argument') ?? '');
    return now(
      message(
        r.ok
          ? `${name} has rolled ${r.total}`
          : `${name} has given incorrect parameters. Please try again. Beep boop.`,
      ),
    );
  },
};

/** Pure-ish command router: no I/O except through the injected callApi (inside deferred.run). */
export function dispatchCommand(interaction: Interaction, callApi: CallApi): CommandResult {
  const handler = HANDLERS[interaction.data?.name ?? ''];
  if (!handler) return now(message('Unknown command.'));
  return handler(interaction, callApi);
}

export const HANDLER_NAMES = Object.keys(HANDLERS);
export const COMMAND_NAMES = COMMANDS.map((c) => c.name);
