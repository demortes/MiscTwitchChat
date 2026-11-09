import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const aiName = new Hono<AppEnv>();

const SYSTEM_PROMPT =
  'You are a helpful assistant. You will be asked to complete things related to certain video games. Provide only short answers, designed to be inserted into a livestream chat, like Twitch chat. Stay casual, but not insulting. Use puns whenever you can.';

// .NET HttpStatusCode names, so error strings match the original ("Error: BadRequest - ...").
const STATUS_NAMES: Record<number, string> = {
  400: 'BadRequest', 401: 'Unauthorized', 403: 'Forbidden', 404: 'NotFound', 408: 'RequestTimeout',
  409: 'Conflict', 413: 'RequestEntityTooLarge', 422: 'UnprocessableEntity', 429: 'TooManyRequests',
  500: 'InternalServerError', 501: 'NotImplemented', 502: 'BadGateway', 503: 'ServiceUnavailable',
  504: 'GatewayTimeout',
};

export function statusName(status: number): string {
  return STATUS_NAMES[status] ?? String(status);
}

export function buildPrompt(game: string, item: string): string {
  return `Generate a short, casual, and punny name for a ${item} within the game ${game}. Say just the name, nothing else.`;
}

aiName.get('/:game/:item', async (c) => {
  const apiKey = c.env.OPEN_AI_KEY;
  const baseUrl = c.env.OPEN_AI_URL;
  if (!apiKey || !baseUrl) {
    return c.text('API key or base URL is not set in environment variables.', 500);
  }
  const game = c.req.param('game');
  const item = c.req.param('item');

  try {
    const resp = await fetch(baseUrl, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${apiKey}`,
        'Content-Type': 'application/json',
        'User-Agent': 'MiscTwitchChat-Worker',
      },
      body: JSON.stringify({
        model: 'gpt-4',
        messages: [
          { role: 'system', content: SYSTEM_PROMPT },
          { role: 'user', content: buildPrompt(game, item) },
        ],
        max_tokens: 100,
      }),
    });
    if (!resp.ok) {
      return c.text(`Error: ${statusName(resp.status)} - ${await resp.text()}`);
    }
    const data = (await resp.json()) as { choices: { message: { content: string } }[] };
    return c.text(data.choices[0].message.content);
  } catch {
    return c.text('Internal Server Error', 500);
  }
});
