export type Bindings = {
  DB: D1Database;
  ASSETS: Fetcher;
  OPEN_AI_KEY: string;
  OPEN_AI_URL: string;
  TwitterApiKey: string;
  TwitterApiSecret: string;
  DictionaryApiKey: string;
  TWITCH_EVENTSUB_SECRET: string;
};

export type AppEnv = { Bindings: Bindings };
