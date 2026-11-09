import { interactionRouter } from '../lib/interact';

export const poke = interactionRouter('poke', (t, o) => `${t} was poke by ${o}. POKE HARDER!`);
