// Twitch EventSub helpers: HMAC signature verification, replay-window check, message-id dedupe.

export const MAX_AGE_MS = 10 * 60 * 1000;

const encoder = new TextEncoder();

function toHex(buf: ArrayBuffer): string {
  return [...new Uint8Array(buf)].map((b) => b.toString(16).padStart(2, '0')).join('');
}

/** Constant-time string comparison (length difference leaks only length, which is public here). */
export function timingSafeEqual(a: string, b: string): boolean {
  const ab = encoder.encode(a);
  const bb = encoder.encode(b);
  let diff = ab.length ^ bb.length;
  const n = Math.max(ab.length, bb.length);
  for (let i = 0; i < n; i++) diff |= (ab[i] ?? 0) ^ (bb[i] ?? 0);
  return diff === 0;
}

/** Returns 'sha256=' + hex(HMAC-SHA256(secret, messageId + timestamp + rawBody)). */
export async function computeSignature(
  secret: string,
  messageId: string,
  timestamp: string,
  rawBody: string,
): Promise<string> {
  const key = await crypto.subtle.importKey(
    'raw',
    encoder.encode(secret),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign'],
  );
  const sig = await crypto.subtle.sign('HMAC', key, encoder.encode(messageId + timestamp + rawBody));
  return 'sha256=' + toHex(sig);
}

export async function verifySignature(
  secret: string,
  messageId: string | undefined,
  timestamp: string | undefined,
  signature: string | undefined,
  rawBody: string,
): Promise<boolean> {
  if (!secret || !messageId || !timestamp || !signature) return false;
  const expected = await computeSignature(secret, messageId, timestamp, rawBody);
  return timingSafeEqual(expected, signature);
}

/** True if the timestamp parses and is no older than 10 minutes (and not absurdly in the future). */
export function isTimestampFresh(timestamp: string | undefined, now: number = Date.now()): boolean {
  if (!timestamp) return false;
  const t = Date.parse(timestamp);
  if (Number.isNaN(t)) return false;
  return Math.abs(now - t) <= MAX_AGE_MS;
}

// Best-effort dedupe. Per-isolate memory only: it is lost on isolate eviction and not shared
// across isolates/colos. That is fine because the D1 upsert is idempotent; this only saves work.
const seen = new Set<string>();
const MAX_SEEN = 2000;

/** Returns true if the id was already seen in this isolate; records it otherwise. */
export function seenBefore(messageId: string): boolean {
  if (seen.has(messageId)) return true;
  if (seen.size >= MAX_SEEN) {
    const oldest = seen.values().next().value;
    if (oldest !== undefined) seen.delete(oldest);
  }
  seen.add(messageId);
  return false;
}

/** Test helper. */
export function resetSeen(): void {
  seen.clear();
}
