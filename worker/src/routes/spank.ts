import { interactionRouter } from '../lib/interact';

export const spank = interactionRouter(
  'spank',
  (t, o) => `Well well well, ${t} was spanked by ${o}. WAS THERE EVEN CONSENT?!`,
);
