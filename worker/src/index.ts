import { Hono } from 'hono';
import type { AppEnv } from './env';
import { cat } from './routes/cat';
import { dad } from './routes/dad';
import { dog } from './routes/dog';
import { insult } from './routes/insult';
import { dictionary } from './routes/dictionary';
import { urban } from './routes/urban';
import { aiName } from './routes/aiName';
import { hug } from './routes/hug';
import { poke } from './routes/poke';
import { spank } from './routes/spank';
import { gift } from './routes/gift';
import { cards } from './routes/cards';
import { commandCounts } from './routes/commandCounts';
import { eightball } from './routes/eightball';
import { health } from './routes/health';
import { docs } from './routes/docs';
import { twitch } from './webhooks/twitch';
import { connect as twitchConnect } from './twitch/connect';
import { discord } from './discord/interactions';

export const app = new Hono<AppEnv>();

// ASP.NET routing was case-insensitive for the controller segment (/api/Hug == /api/hug).
// Lowercase ONLY that prefix; path params such as channel and user names keep their case.
export function normalizeApiPrefix(pathname: string): string {
  return pathname.replace(/^\/api\/(ai\/)?([^/]+)/i, (_m, ai: string | undefined, name: string) =>
    `/api/${ai ? 'ai/' : ''}${name.toLowerCase()}`);
}

app.route('/api/cat', cat);
app.route('/api/dad', dad);
app.route('/api/dog', dog);
app.route('/api/insult', insult);
app.route('/api/dictionary', dictionary);
app.route('/api/urban', urban);
app.route('/api/ai/name', aiName);
app.route('/api/hug', hug);
app.route('/api/poke', poke);
app.route('/api/spank', spank);
app.route('/api/gift', gift);
app.route('/api/cards', cards);
app.route('/api/commandcounts', commandCounts);
app.route('/api/eightball', eightball);
app.route('/health', health);
app.route('/', docs); // swagger / openapi.json
app.route('/twitch', twitch);
app.route('/twitch', twitchConnect); // /twitch/connect + /twitch/callback
app.route('/discord', discord);

export default {
  fetch(request: Request, env: AppEnv['Bindings'], ctx: ExecutionContext) {
    const url = new URL(request.url);
    const normalized = normalizeApiPrefix(url.pathname);
    if (normalized !== url.pathname) {
      url.pathname = normalized;
      request = new Request(url, request);
    }
    return app.fetch(request, env, ctx);
  },
};
