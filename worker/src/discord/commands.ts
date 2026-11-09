// Single source of truth for the slash commands. Used by the router (dispatch.ts)
// and by scripts/register-commands.ts. Names/descriptions/options mirror the
// original Discord.Net modules in DiscordBot/Modules/*.cs.
// Option names follow Discord.Net's convention (lower-cased C# parameter names);
// Discord.Net used the parameter name as the option description.

export const OptionType = {
  String: 3,
  Integer: 4,
  Boolean: 5,
  User: 6,
} as const;

export interface CommandOptionChoice {
  name: string;
  value: string | number;
}

export interface CommandOption {
  type: (typeof OptionType)[keyof typeof OptionType];
  name: string;
  description: string;
  required?: boolean;
  choices?: CommandOptionChoice[];
}

export interface CommandDefinition {
  name: string;
  description: string;
  type: 1; // CHAT_INPUT
  options?: CommandOption[];
}

const slash = (name: string, description: string, options?: CommandOption[]): CommandDefinition =>
  options ? { name, description, type: 1, options } : { name, description, type: 1 };

export const COMMANDS: readonly CommandDefinition[] = [
  // BlameModule
  slash('blame', 'Blame someone.... anyone...', [
    { type: OptionType.User, name: 'target', description: 'target', required: true },
  ]),
  slash('blamedem', 'Demortes has his own blame command...'),
  slash('blamecody', 'Australians get one too....'),
  // CardsAgainstHumanityModule
  slash('ping', 'Is it alive?'),
  slash('userinfo', 'Get some information about a user.', [
    { type: OptionType.User, name: 'user', description: 'user', required: false },
  ]),
  slash('cah', 'Everyones favorite card game....', [
    { type: OptionType.String, name: 'args', description: 'args', required: false },
  ]),
  slash('bancah', "Ban everyone's favorite card game."),
  // CatModule
  slash('cat', 'Get a fact about one of the worlds favorite animals.', [
    { type: OptionType.User, name: 'target', description: 'target', required: false },
  ]),
  // DadModule
  slash('dad', 'Summon the inner dad and make a joke.'),
  // DogModule
  slash('dog', 'Facts about woofers.'),
  // InsultModule
  slash('insult', 'Insult someone. Either tag them or let the bot pick one randomly.', [
    { type: OptionType.User, name: 'target', description: 'target', required: false },
  ]),
  // NameModule
  slash(
    'generate-name',
    'Generate a name to be used in a video game. Pick the game, and the object you want a name for.',
    [
      { type: OptionType.String, name: 'game', description: 'game', required: true },
      { type: OptionType.String, name: 'item', description: 'item', required: true },
    ],
  ),
  // RollModule
  slash('roll', 'Roll a number of dice, using the XdY format, ex: 1d20', [
    { type: OptionType.String, name: 'argument', description: 'argument', required: true },
  ]),
];
