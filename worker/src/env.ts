export type Bindings = {
  DB: D1Database;
  ASSETS: Fetcher;
  OPEN_AI_KEY: string;
  OPEN_AI_URL: string;
  OPEN_AI_MODEL?: string;
  DictionaryApiKey: string;
  TWITCH_EVENTSUB_SECRET: string;
  TWITCH_CLIENT_ID: string;
  TWITCH_CLIENT_SECRET: string;
  TWITCH_BOT_USER_ID: string;
  DISCORD_PUBLIC_KEY: string;
  DISCORD_APP_ID: string;
};

export type AppEnv = { Bindings: Bindings };
