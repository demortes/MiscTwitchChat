// OpenAPI 3 description of the API. Most operations return a single text/plain string, matching the
// handlers in src/routes.

type Param = { name: string; in: 'path' | 'query'; required: boolean; schema: { type: string }; description?: string };

const path = (name: string, description?: string): Param => ({ name, in: 'path', required: true, schema: { type: 'string' }, description });
const query = (name: string, required: boolean, description?: string): Param => ({ name, in: 'query', required, schema: { type: 'string' }, description });

const text = (description: string, status = '200') => ({
  [status]: { description, content: { 'text/plain': { schema: { type: 'string' } } } },
});

function get(tag: string, operationId: string, summary: string, parameters: Param[] = [], extra: Record<string, unknown> = {}) {
  return { get: { tags: [tag], operationId, summary, parameters, responses: text('Success'), ...extra } };
}

export const openapi = {
  openapi: '3.0.1',
  info: {
    title: "Demortes' Random Chat Bot APIs",
    version: 'v1',
    description: "A collection of APIs for Demortes' Twitch chat bot.",
    contact: { name: 'Demortes', url: 'https://demortes.com', email: 'webmaster@demortes.com' },
    license: { name: 'MIT License', url: 'https://opensource.org/licenses/MIT' },
  },
  paths: {
    '/api/cat': get('Cat', 'CatGet', 'Gets a random cat fact.'),
    '/api/dad': get('Dad', 'DadGet', 'Gets a random dad joke.'),
    '/api/dog': get('Dog', 'DogGet', 'Gets a random dog fact.'),
    '/api/insult': get('Insult', 'InsultGet', 'Gets an insult, optionally aimed at a target.', [
      query('channel', false, 'The channel the insult is being used in.'),
      query('user', false, 'The user using the insult.'),
      query('target', false, 'The target of the insult. If not specified, the insult will be generic.'),
    ]),
    '/api/dictionary/{word}': get('Dictionary', 'DictionaryGet', 'Gets a short definition of a word.', [path('word')], {
      responses: { ...text('Success'), ...text('word is required', '400'), ...text('Not Found', '404') },
    }),
    '/api/urban/{term}': get('Urban', 'UrbanGet', 'Gets the Urban Dictionary definition of a term.', [path('term')]),
    '/api/tweet/{twitterUsername}': get('Tweet', 'TweetGet', "Gets a user's latest tweet.", [path('twitterUsername')]),
    '/api/ai/name/{game}/{item}': get('Name', 'NameGet', 'Generates an AI name for an item in a game.', [path('game'), path('item')]),
    '/api/hug/{channel}/{origUser}': get('Hugs', 'HugsHug', 'Hugs a random consenting chatter.', [path('channel'), path('origUser')]),
    '/api/hug/{channel}/{origUser}/consent': get('Hugs', 'HugsConsent', 'Toggles whether a user consents to being hugged.', [path('channel'), path('origUser')]),
    '/api/poke/{channel}/{origUser}': get('Poke', 'PokePoke', 'Pokes a random consenting chatter.', [path('channel'), path('origUser')]),
    '/api/poke/{channel}/{origUser}/consent': get('Poke', 'PokeConsent', 'Toggles whether a user consents to being poked.', [path('channel'), path('origUser')]),
    '/api/spank/{channel}/{origUser}': get('Spank', 'SpankSpank', 'Spanks a random consenting chatter.', [path('channel'), path('origUser')]),
    '/api/spank/{channel}/{origUser}/consent': get('Spank', 'SpankConsent', 'Toggles whether a user consents to being spanked.', [path('channel'), path('origUser')]),
    '/api/gift': get('Gift', 'GiftGet', 'Gifts a random item to a user in a channel.', [
      query('channel', true, 'The channel the gift is being given in.'),
      query('fromUser', true, 'The user giving the gift.'),
      query('giftingUsername', false, 'The user receiving the gift. If not specified, a random user will be chosen.'),
    ]),
    '/api/cards': get('Cards', 'CardsGet', 'Gets a random Cards Against Humanity prompt.'),
    '/api/cards/{channel}': {
      ...get('Cards', 'CardsGetForChannel', 'Gets a random Cards Against Humanity prompt, honoring a channel ban.', [path('channel')]),
      delete: {
        tags: ['Cards'],
        operationId: 'CardsDelete',
        summary: 'Removes the Cards Against Humanity ban for a channel.',
        parameters: [path('channel')],
        responses: text('Success'),
      },
    },
    '/api/cards/ban/{channel}': get('Cards', 'CardsBan', 'Temporarily bans Cards Against Humanity in a channel.', [path('channel')]),
    '/api/commandcounts/{channel}/{targetUser}/{commandUsed}': {
      get: {
        tags: ['CommandCounts'],
        operationId: 'CommandCountsGet',
        summary: 'Gets how many times a command was used against a user in a channel.',
        parameters: [path('channel'), path('targetUser'), path('commandUsed')],
        responses: { '200': { description: 'Success', content: { 'text/plain': { schema: { type: 'integer', format: 'int32' } } } } },
      },
    },
    '/api/eightball': get('EightBall', 'EightBallGet', 'Gets a random Magic 8-Ball response.'),
  },
};
