import { Hono } from 'hono';
import type { AppEnv } from '../env';

export const tweet = new Hono<AppEnv>();

tweet.get('/:twitterUsername', async (c) => {
  const user = c.req.param('twitterUsername');
  const env = c.env as unknown as Record<string, string | undefined>;
  try {
    const apiKey = c.env.TwitterApiKey.trim();
    const apiSecret = (env.TwitterApiSecret ?? '').trim();
    const basic = btoa(`${apiKey}:${apiSecret}`);

    const tokenResp = await fetch('https://api.twitter.com/oauth2/token', {
      method: 'POST',
      headers: {
        Authorization: `Basic ${basic}`,
        'Content-Type': 'application/x-www-form-urlencoded',
        'User-Agent': 'MiscTwitchChat-Worker',
      },
      body: new URLSearchParams({ grant_type: 'client_credentials' }).toString(),
    });
    const token = ((await tokenResp.json()) as { access_token: string }).access_token;

    const tl = await fetch(
      `https://api.twitter.com/1.1/statuses/user_timeline.json?screen_name=${encodeURIComponent(user)}&count=1&exclude_replies=true&trim_user=true&include_rts=false`,
      { headers: { Authorization: `Bearer ${token}`, 'User-Agent': 'MiscTwitchChat-Worker' } },
    );
    if (!tl.ok) return c.text('Internal Server Error', 500);
    // Parse the id from raw text: tweet ids exceed JS safe-integer range.
    const raw = await tl.text();
    const m = /"id_str"\s*:\s*"(\d+)"/.exec(raw) ?? /"id"\s*:\s*(\d+)/.exec(raw);
    if (!m) return c.text('Internal Server Error', 500);
    return c.text(`https://twitter.com/${user}/status/${m[1]}`);
  } catch {
    return c.text('Internal Server Error', 500);
  }
});
