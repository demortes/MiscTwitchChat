import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const eightball = new Hono<AppEnv>();

export const RESPONSES: string[] = [
  "Yes, of course!",
  "Without a doubt, yes.",
  "You can count on it.",
  "For sure!",
  "Ask me later.",
  "I'm not sure.",
  "I can't tell you right now.",
  "I'll tell you after my nap.",
  "No way!",
  "Doesn't look great.",
  "I don't think so.",
  "Without a doubt, no.",
  "The answer is clearly NO!",
  "Today doesn't look good. Tomorrow doesn't either. Nor the year for that fact.",
  "Only I will ever know",
  "I better not say.",
  "Repeat after me... I am not that lucky.",
  "Only if your name is Zach.",
  "What are you? Stupid?",
  "What the hell kind of question is that?",
  "HAH! In your dreams.",
  "Sure, why not?",
  "Ra made me say yes.",
  "Only if you eat smegma.",
  "Help me.... The oompa loompa uses me when he decides to go to war...",
];

// Mirrors EightBallController: Random.Next(0, Length - 1) never selects the last element.
eightball.get('/', (c) => c.text(RESPONSES[Math.floor(Math.random() * (RESPONSES.length - 1))]));
