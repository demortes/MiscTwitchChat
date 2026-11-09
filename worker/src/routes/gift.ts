import { Hono } from 'hono';
import type { AppEnv } from '../env';
import { getRandomConsentingChatter, NO_CHATTERS_MESSAGE, pickRandom } from '../lib/chatters';
import { incrementCommandCount } from '../lib/counts';
import { gifts } from '../data/gifts';

export const gift = new Hono<AppEnv>();

gift.get('/', async (c) => {
  const channel = c.req.query('channel');
  const fromUser = c.req.query('fromUser');
  let giftingUsername = c.req.query('giftingUsername');
  if (!channel || !fromUser) return c.text('channel and fromUser are required.', 400);

  if (!giftingUsername || giftingUsername === fromUser) {
    giftingUsername = (await getRandomConsentingChatter(c.env.DB, channel, fromUser)) ?? undefined;
    if (!giftingUsername) return c.text(NO_CHATTERS_MESSAGE);
  }
  // Original counts the gift against the giver (TargetUser == fromUser); preserved.
  await incrementCommandCount(c.env.DB, channel, fromUser, 'gift');
  const item = pickRandom(gifts) ?? 'nothing';
  return c.text(`${fromUser} hands over a gift of ${item} to ${giftingUsername}`);
});
