import { Hono } from 'hono';
import type { AppEnv } from '../env';
import { getRandomConsentingChatter, isDisconsenter, NO_CHATTERS_MESSAGE, toggleConsent } from './chatters';
import { incrementCommandCount } from './counts';

/** Builds the shared hug/poke/spank router: GET /:channel/:origUser and GET /:channel/:origUser/consent. */
export function interactionRouter(
  command: string,
  message: (target: string, origUser: string) => string,
): Hono<AppEnv> {
  const r = new Hono<AppEnv>();
  r.get('/:channel/:origUser', async (c) => {
    const { channel, origUser } = c.req.param();
    if (await isDisconsenter(c.env.DB, origUser)) {
      return c.text(`${origUser} does not consent and so is not allowed to ${command}.`);
    }
    const target = await getRandomConsentingChatter(c.env.DB, channel, origUser);
    if (!target) return c.text(NO_CHATTERS_MESSAGE);
    await incrementCommandCount(c.env.DB, channel, target, command);
    return c.text(message(target, origUser));
  });
  r.get('/:channel/:origUser/consent', async (c) => c.text(await toggleConsent(c.env.DB, c.req.param('origUser'))));
  return r;
}
