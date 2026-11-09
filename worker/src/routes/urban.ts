import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const urban = new Hono<AppEnv>();

export const MAX_LEN = 175;
export const DEMORTES_MSG =
  "An awesome individual who doesn't need his own Urban Dictionary entry. Creator of this API. Nice try chump.";
export const CHRISSIE_MSG = "LittleChrissie can't remove this either, but that's OK. They're pretty great.";
export const ERROR_MSG = 'ABORT ABORT ABORT: Server error. Something broke, but not likely your bot.';

/** Replaces line breaks with spaces and truncates to 175 chars, appending '...' if cut. */
export function formatDefinition(text: string): string {
  const full = text.replace(/\r\n/g, ' ').replace(/\r/g, ' ').replace(/\n/g, ' ');
  return full.slice(0, MAX_LEN) + (full.length > MAX_LEN ? '...' : '');
}

interface UrbanDef {
  definition: string;
  thumbs_up?: number;
  thumbs_down?: number;
}

/** Resolves the plain-text response for a term (never throws). */
export async function lookup(term: string): Promise<string> {
  const lower = term.toLowerCase();
  if (lower === 'demortes') return DEMORTES_MSG;
  if (lower === 'littlechrissie') return CHRISSIE_MSG;
  try {
    const resp = await fetch(`https://api.urbandictionary.com/v0/define?term=${encodeURIComponent(term)}`, {
      headers: { 'User-Agent': 'MiscTwitchChat-Worker' },
    });
    if (!resp.ok) return ERROR_MSG;
    const body = (await resp.json()) as { list?: UrbanDef[] };
    const defs = body.list ?? [];
    if (defs.length < 1) return `404: ${term} not found.`;
    const score = (d: UrbanDef) => (d.thumbs_up ?? 0) - (d.thumbs_down ?? 0);
    // Stable sort descending by vote; first wins ties (same as OrderByDescending).
    const best = [...defs].sort((a, b) => score(b) - score(a))[0];
    return formatDefinition(best.definition);
  } catch {
    return ERROR_MSG;
  }
}

urban.get('/:term', async (c) => c.text(await lookup(c.req.param('term'))));
