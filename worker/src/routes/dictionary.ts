import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const dictionary = new Hono<AppEnv>();

export interface WordEntry {
  shortdef?: string[];
  fl?: string;
  def?: { vd?: string }[];
}

/** Picks the meaning from the first entry: shortdef[0], else fl ?? def[0].vd. */
export function pickMeaning(entries: WordEntry[]): string | null {
  const first = entries[0];
  let meaning: string | null | undefined = first.shortdef?.[0];
  if ((meaning == null || meaning.trim() === '') && first.def && first.def.length > 0) {
    meaning = first.fl ?? first.def[0].vd;
  }
  if (meaning == null || meaning.trim() === '') return null;
  return meaning;
}

dictionary.get('/:word', async (c) => {
  const word = c.req.param('word');
  if (!word || word.trim() === '') return c.text('word is required', 400);

  // Original reads Dictionary:ApiKey or DictionaryApiKey from configuration.
  const env = c.env as unknown as Record<string, string | undefined>;
  const key = env.DictionaryApiKey ?? env.DICTIONARY_API_KEY ?? '';
  const url = `https://www.dictionaryapi.com/api/v3/references/collegiate/json/${encodeURIComponent(word)}?key=${encodeURIComponent(key)}`;

  try {
    const resp = await fetch(url, { headers: { 'User-Agent': 'MiscTwitchChat-Worker' } });
    if (!resp.ok) return new Response(null, { status: resp.status });

    const entries = (await resp.json()) as unknown;
    if (!Array.isArray(entries) || entries.length === 0) return c.text('Not Found', 404);
    // A list of suggestion strings fails deserialization in the original (500).
    if (typeof entries[0] !== 'object' || entries[0] === null) {
      return c.text('Internal Server Error', 500);
    }
    const meaning = pickMeaning(entries as WordEntry[]);
    if (meaning === null) return c.text('Not Found', 404);
    return c.text(meaning);
  } catch {
    return c.text('Internal Server Error', 500);
  }
});
