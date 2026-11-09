// Ed25519 verification of Discord interaction requests using WebCrypto.

function hexToBytes(hex: string): Uint8Array | null {
  if (hex.length === 0 || hex.length % 2 !== 0 || !/^[0-9a-fA-F]+$/.test(hex)) return null;
  const out = new Uint8Array(hex.length / 2);
  for (let i = 0; i < out.length; i++) out[i] = parseInt(hex.slice(i * 2, i * 2 + 2), 16);
  return out;
}

/** Returns true only if signature is a valid Ed25519 signature of timestamp + rawBody. */
export async function verifyDiscordSignature(
  publicKeyHex: string | undefined,
  signatureHex: string | null | undefined,
  timestamp: string | null | undefined,
  rawBody: string,
): Promise<boolean> {
  if (!publicKeyHex || !signatureHex || !timestamp) return false;
  const key = hexToBytes(publicKeyHex);
  const sig = hexToBytes(signatureHex);
  if (!key || !sig || key.length !== 32 || sig.length !== 64) return false;
  try {
    const cryptoKey = await crypto.subtle.importKey('raw', key, { name: 'Ed25519' }, false, ['verify']);
    return await crypto.subtle.verify('Ed25519', cryptoKey, sig, new TextEncoder().encode(timestamp + rawBody));
  } catch {
    return false;
  }
}
