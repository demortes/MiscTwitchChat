/**
 * Registers (bulk-overwrites) all slash commands with Discord.
 *
 * Usage (from worker/):
 *   DISCORD_APP_ID=... DISCORD_BOT_TOKEN=... npx vite-node scripts/register-commands.ts
 *   # optional: DISCORD_GUILD_ID=... registers to one guild instantly (global commands can take up to an hour)
 *
 * PowerShell:
 *   $env:DISCORD_APP_ID="..."; $env:DISCORD_BOT_TOKEN="..."; npx vite-node scripts/register-commands.ts
 *
 * (vite-node ships with vitest, so no extra install is needed.)
 * The command list comes from src/discord/commands.ts, the same list the router uses.
 */
import { COMMANDS } from '../src/discord/commands';

const appId = process.env.DISCORD_APP_ID;
const token = process.env.DISCORD_BOT_TOKEN;
const guildId = process.env.DISCORD_GUILD_ID;

if (!appId || !token) {
  console.error('DISCORD_APP_ID and DISCORD_BOT_TOKEN must be set.');
  process.exit(1);
}

const base = `https://discord.com/api/v10/applications/${appId}`;
const url = guildId ? `${base}/guilds/${guildId}/commands` : `${base}/commands`;

const res = await fetch(url, {
  method: 'PUT',
  headers: { Authorization: `Bot ${token}`, 'Content-Type': 'application/json' },
  body: JSON.stringify(COMMANDS),
});

if (!res.ok) {
  console.error(`Discord returned ${res.status}: ${await res.text()}`);
  process.exit(1);
}

const registered = (await res.json()) as { name: string }[];
console.log(`Registered ${registered.length} commands ${guildId ? `to guild ${guildId}` : 'globally'}:`);
for (const c of registered) console.log(`  /${c.name}`);
