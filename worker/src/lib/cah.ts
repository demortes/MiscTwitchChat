export interface CahData {
  blackCards: { text: string; pick: number }[];
  whiteCards: string[];
}

const NAMED: Record<string, string> = {
  amp: '&', lt: '<', gt: '>', quot: '"', apos: "'", nbsp: ' ', reg: '®', trade: '™',
  copy: '©', ndash: '–', mdash: '—', hellip: '…', larr: '←', rarr: '→',
  uarr: '↑', darr: '↓', eacute: 'é', iacute: 'í', ntilde: 'ñ', Uuml: 'Ü',
  uuml: 'ü', ouml: 'ö', Ouml: 'Ö', auml: 'ä', Auml: 'Ä', aacute: 'á',
  oacute: 'ó', uacute: 'ú', egrave: 'è', agrave: 'à', ccedil: 'ç',
  lsquo: '‘', rsquo: '’', ldquo: '“', rdquo: '”', deg: '°', hearts: '♥',
};

export function htmlDecode(s: string): string {
  return s.replace(/&(#x[0-9a-fA-F]+|#[0-9]+|[a-zA-Z][a-zA-Z0-9]*);/g, (m, ent: string) => {
    if (ent[0] === '#') {
      const cp = ent[1] === 'x' || ent[1] === 'X' ? parseInt(ent.slice(2), 16) : parseInt(ent.slice(1), 10);
      return Number.isFinite(cp) && cp >= 0 && cp <= 0x10ffff ? String.fromCodePoint(cp) : m;
    }
    return NAMED[ent] ?? m;
  });
}

/** HtmlDecode then replace every `<...>` tag with a space (mirrors CardsController.StripHTML). */
export function stripHTML(input: string): string {
  return htmlDecode(input).replace(/<.*?>/g, ' ');
}

/** Replace the first occurrence of `search` (literal) in `text`; unchanged if absent. */
export function replaceFirst(text: string, search: string, replacement: string): string {
  const pos = text.indexOf(search);
  if (pos < 0) return text;
  return text.slice(0, pos) + replacement + text.slice(pos + search.length);
}

/** Assemble a black card with `pick` white cards (first '_' replaced per card, else appended). */
export function buildCahCard(data: CahData, rand: () => number = Math.random): string {
  const idx = (n: number) => Math.min(n - 1, Math.floor(rand() * n));
  const black = data.blackCards[idx(data.blackCards.length)];
  let out = stripHTML(black.text);
  for (let i = 0; i < black.pick; i++) {
    const white = data.whiteCards[idx(data.whiteCards.length)];
    const wrapped = `*${stripHTML(white)}*`;
    out = out.includes('_') ? replaceFirst(out, '_', wrapped) : out + wrapped;
  }
  return out;
}
