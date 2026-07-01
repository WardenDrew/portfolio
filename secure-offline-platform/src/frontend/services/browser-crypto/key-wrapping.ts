import { aesGcmKeyLengthBits } from './constants';
import { base64Decode, base64Encode } from './encoding-random';

export async function wrapAesGcmDataKey(dataKey: CryptoKey, wrappingPublicKey: CryptoKey): Promise<string> {
  const encryptedDataKey = await crypto.subtle.wrapKey('raw', dataKey, wrappingPublicKey, { name: 'RSA-OAEP' });
  return base64Encode(encryptedDataKey);
}

export async function unwrapAesGcmDataKey(
  encryptedDataKey: string,
  wrappingPrivateKey: CryptoKey,
  usages: KeyUsage[],
): Promise<CryptoKey> {
  return crypto.subtle.unwrapKey(
    'raw',
    base64Decode(encryptedDataKey),
    wrappingPrivateKey,
    { name: 'RSA-OAEP' },
    { name: 'AES-GCM', length: aesGcmKeyLengthBits },
    false,
    usages,
  );
}
