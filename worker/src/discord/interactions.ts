import { Hono } from 'hono';
import type { AppEnv } from '../env';
import { dispatchCommand, message, type CallApi, type Interaction, type MessageData } from './dispatch';
import { verifyDiscordSignature } from './verify';

export const discord = new Hono<AppEnv>();

const DISCORD_API = 'https://discord.com/api/v10';

const InteractionType = { Ping: 1, ApplicationCommand: 2 } as const;
const ResponseType = { Pong: 1, ChannelMessage: 4, Deferred: 5 } as const;

/** Calls the Worker's own HTTP routes in-process (lazy import avoids the index.ts <-> this file cycle). */
function internalApi(env: AppEnv['Bindings'], ctx: Parameters<Hono<AppEnv>['fetch']>[2]): CallApi {
  return async (path) => {
    const { app } = await import('../index');
    const res = await app.fetch(new Request(`https://internal${path}`), env, ctx);
    const text = await res.text();
    if (!res.ok) throw new Error(`API ${path} returned ${res.status}: ${text.slice(0, 200)}`);
    return text;
  };
}

async function discordRequest(url: string, method: string, body?: unknown): Promise<void> {
  const res = await fetch(url, {
    method,
    headers: { 'content-type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!res.ok) console.error('discord webhook call failed', method, res.status, (await res.text()).slice(0, 200));
}

/** Completes a deferred (type 5) interaction with the final message. */
export async function sendDeferredResult(interaction: Interaction, data: MessageData): Promise<void> {
  const base = `${DISCORD_API}/webhooks/${interaction.application_id}/${interaction.token}`;
  if (data.tts) {
    // Editing the original cannot set TTS; post a followup with tts and drop the "thinking" placeholder.
    await discordRequest(base, 'POST', data);
    await discordRequest(`${base}/messages/@original`, 'DELETE');
  } else {
    await discordRequest(`${base}/messages/@original`, 'PATCH', data);
  }
}

discord.post('/interactions', async (c) => {
  const rawBody = await c.req.text();
  const ok = await verifyDiscordSignature(
    c.env.DISCORD_PUBLIC_KEY,
    c.req.header('X-Signature-Ed25519'),
    c.req.header('X-Signature-Timestamp'),
    rawBody,
  );
  if (!ok) return c.text('invalid request signature', 401);

  let interaction: Interaction;
  try {
    interaction = JSON.parse(rawBody) as Interaction;
  } catch {
    return c.text('bad request', 400);
  }

  if (interaction.type === InteractionType.Ping) return c.json({ type: ResponseType.Pong });
  if (interaction.type !== InteractionType.ApplicationCommand) return c.text('unsupported interaction', 400);

  const result = dispatchCommand(interaction, internalApi(c.env, c.executionCtx));
  if (result.kind === 'immediate') return c.json({ type: ResponseType.ChannelMessage, data: result.data });

  c.executionCtx.waitUntil(
    result
      .run()
      .catch((err) => {
        console.error('deferred command failed', err);
        return message('Something went wrong. Please DM Demortes');
      })
      .then((data) => sendDeferredResult(interaction, data))
      .catch((err) => console.error('failed to send deferred result', err)),
  );
  return c.json({ type: ResponseType.Deferred });
});

/**
 * Where the site's "Add my Discord bot" link points. The application ID comes from the DISCORD_APP_ID secret, so
 * the repo holds no Discord identifier and each deployment invites its own application.
 */
discord.get('/invite', (c) => {
  const appId = c.env.DISCORD_APP_ID;
  if (!appId) return c.text('Discord is not configured on this instance.', 404);
  return c.redirect(
    `https://discord.com/oauth2/authorize?client_id=${encodeURIComponent(appId)}&scope=applications.commands`,
    302,
  );
});
