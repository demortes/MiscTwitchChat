import { describe, expect, it } from 'vitest';
import { pickConsentingChatter, pickRandom } from '../src/lib/chatters';
import { buildCahCard, replaceFirst, stripHTML } from '../src/lib/cah';
import { gifts } from '../src/data/gifts';

describe('pickRandom', () => {
  it('returns undefined for empty list without throwing', () => {
    expect(pickRandom([])).toBeUndefined();
  });
  it('is uniform over ALL items, including the last', () => {
    const items = ['a', 'b', 'c'];
    expect(pickRandom(items, () => 0)).toBe('a');
    expect(pickRandom(items, () => 0.5)).toBe('b');
    expect(pickRandom(items, () => 0.999999)).toBe('c');
    expect(pickRandom(items, () => 1)).toBe('c'); // defensive clamp
  });
});

describe('pickConsentingChatter', () => {
  it('returns null on empty or fully-excluded lists', () => {
    expect(pickConsentingChatter([], [], 'me')).toBeNull();
    expect(pickConsentingChatter(['me', 'bob'], ['Bob'], 'ME')).toBeNull();
  });
  it('excludes disconsenters and the origin user (case-insensitive) and dedupes', () => {
    const out = new Set<string>();
    for (const r of [0, 0.34, 0.67, 0.99]) {
      out.add(pickConsentingChatter(['me', 'bob', 'Bob', 'carol', 'dave', 'eve'], ['EVE'], 'Me', () => r)!);
    }
    expect([...out].sort()).toEqual(['bob', 'carol', 'dave']);
  });
  it('can pick the last candidate', () => {
    expect(pickConsentingChatter(['a', 'b'], [], 'x', () => 0.99)).toBe('b');
  });
});

describe('CAH assembly', () => {
  it('stripHTML decodes entities and turns tags into spaces', () => {
    expect(stripHTML('Fish &amp; chips<br>now &reg; &#65;&#x42;')).toBe('Fish & chips now ® AB');
    expect(stripHTML('a <i>b</i> c')).toBe('a  b  c');
  });
  it('replaceFirst only replaces the first occurrence', () => {
    expect(replaceFirst('_ and _', '_', 'X')).toBe('X and _');
    expect(replaceFirst('none', '_', 'X')).toBe('none');
  });
  it('fills blanks in order, appends when no blank remains', () => {
    const data = { blackCards: [{ text: '_ + _ = ?', pick: 3 }], whiteCards: ['one', 'two<br>', 'three'] };
    const seq = [0, 0, 0.4, 0.99]; // black, white1, white2, white3
    let i = 0;
    expect(buildCahCard(data, () => seq[i++])).toBe('*one* + *two * = ?*three*');
  });
  it('handles pick=1 with a single blank', () => {
    const data = { blackCards: [{ text: "I got 99 problems but _ ain't one.", pick: 1 }], whiteCards: ['bees'] };
    expect(buildCahCard(data, () => 0)).toBe("I got 99 problems but *bees* ain't one.");
  });
});

describe('gifts data', () => {
  it('is non-empty and has no blanks', () => {
    expect(gifts.length).toBeGreaterThan(100);
    expect(gifts.every((g) => g.length > 0)).toBe(true);
  });
});
