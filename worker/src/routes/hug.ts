import { interactionRouter } from '../lib/interact';

export const hug = interactionRouter(
  'hug',
  (t, o) => `Well well well, ${t} was hugged by ${o}. WAS THERE EVEN CONSENT?!`,
);
