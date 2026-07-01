import {
  finishOpaqueRegistration,
  type OpaqueClientStartState,
  type OpaqueRegistrationStartResponse
} from "../../../opaque-ts/src";
import { generateMnemonic } from "@scure/bip39";
import { wordlist } from "@scure/bip39/wordlists/english.js";

const textEncoder = new TextEncoder();

export type CryptoKeyUpload = {
  cryptoId: string;
  algorithmSuite: string;
  publicKeyFormat: string;
  signingPublicKeyBase64: string;
  encryptionPublicKeyBase64: string;
  publicKeyFingerprint: string;
};

export type ClientWrappedKeyUpload = {
  wrapKind: "password" | "public_key" | "recovery_phrase";
  recipientCryptoId: string | null;
  ciphertextBase64: string;
  envelopeJson: string;
  localSecretFingerprint: string | null;
};

export type PublicKeyWrapRecipient = Pick<
  CryptoKeyUpload,
  | "algorithmSuite"
  | "cryptoId"
  | "encryptionPublicKeyBase64"
  | "publicKeyFingerprint"
  | "publicKeyFormat"
>;

export type RecoveryPhraseCryptoSetup = {
  cryptoKey: CryptoKeyUpload;
  publicKeyFingerprint: string;
  recoveryPhrase: string;
  wrappedKeys: ClientWrappedKeyUpload[];
};

export type RegistrationCryptoSetup = RecoveryPhraseCryptoSetup & {
  opaqueRegistrationRecordJson: string;
};

type ClientPrivateKeyBundle = {
  cryptoKey: {
    cryptoId: string;
    algorithmSuite: string;
    publicKeyFormat: string;
    signingPublicKeyBase64: string;
    encryptionPublicKeyBase64: string;
    publicKeyFingerprint: string;
  };
  privateKeyBundle: string;
  privateKeyBundleFormat: string;
  publicKeyFingerprint: string;
};

export async function createRegistrationCryptoSetup(
  opaqueState: OpaqueClientStartState,
  opaqueStartResponse: OpaqueRegistrationStartResponse
): Promise<RegistrationCryptoSetup> {
  const keyBundle = await createClientPrivateKeyBundle(
    "encryptedworkspace.user-private-key-bundle.v1"
  );
  const recoveryPhrase = createRecoveryPhrase();
  const recoveryPhraseFingerprint = await createFingerprint(
    textEncoder.encode(recoveryPhrase)
  );
  const opaqueRegistration = await finishOpaqueRegistration(
    opaqueState,
    opaqueStartResponse
  );

  return {
    cryptoKey: keyBundle.cryptoKey,
    opaqueRegistrationRecordJson: opaqueRegistration.registrationRecordJson,
    publicKeyFingerprint: keyBundle.publicKeyFingerprint,
    recoveryPhrase,
    wrappedKeys: [
      await wrapPrivateKeyBundleWithRawKey(
        "password",
        opaqueRegistration.passwordWrapKeyBase64,
        keyBundle.privateKeyBundle,
        keyBundle.privateKeyBundleFormat,
        null
      ),
      await wrapPrivateKeyBundle(
        "recovery_phrase",
        recoveryPhrase,
        keyBundle.privateKeyBundle,
        keyBundle.privateKeyBundleFormat,
        recoveryPhraseFingerprint
      )
    ]
  };
}

export async function createRecoveryPhraseCryptoSetup(
  privateKeyBundleFormat = "encryptedworkspace.private-key-bundle.v1",
  publicKeyRecipients: PublicKeyWrapRecipient[] = []
): Promise<RecoveryPhraseCryptoSetup> {
  const keyBundle = await createClientPrivateKeyBundle(privateKeyBundleFormat);
  const recoveryPhrase = createRecoveryPhrase();
  const recoveryPhraseFingerprint = await createFingerprint(
    textEncoder.encode(recoveryPhrase)
  );
  const wrappedKeys: ClientWrappedKeyUpload[] = [
    await wrapPrivateKeyBundle(
      "recovery_phrase",
      recoveryPhrase,
      keyBundle.privateKeyBundle,
      keyBundle.privateKeyBundleFormat,
      recoveryPhraseFingerprint
    )
  ];

  for (const recipient of publicKeyRecipients) {
    wrappedKeys.push(
      await wrapPrivateKeyBundleWithPublicKey(
        recipient,
        keyBundle.privateKeyBundle,
        keyBundle.privateKeyBundleFormat
      )
    );
  }

  return {
    cryptoKey: keyBundle.cryptoKey,
    publicKeyFingerprint: keyBundle.publicKeyFingerprint,
    recoveryPhrase,
    wrappedKeys
  };
}

export function createTotpSecret() {
  return toBase32(getRandomBytes(20));
}

async function createClientPrivateKeyBundle(
  privateKeyBundleFormat: string
): Promise<ClientPrivateKeyBundle> {
  const signingKeyPair = await crypto.subtle.generateKey(
    {
      name: "ECDSA",
      namedCurve: "P-256"
    },
    true,
    ["sign", "verify"]
  );
  const encryptionKeyPair = await crypto.subtle.generateKey(
    {
      name: "ECDH",
      namedCurve: "P-256"
    },
    true,
    ["deriveBits", "deriveKey"]
  );

  const signingPublicKey = await crypto.subtle.exportKey(
    "spki",
    signingKeyPair.publicKey
  );
  const signingPrivateKey = await crypto.subtle.exportKey(
    "pkcs8",
    signingKeyPair.privateKey
  );
  const encryptionPublicKey = await crypto.subtle.exportKey(
    "spki",
    encryptionKeyPair.publicKey
  );
  const encryptionPrivateKey = await crypto.subtle.exportKey(
    "pkcs8",
    encryptionKeyPair.privateKey
  );
  const publicKeyFingerprint = await createFingerprint(
    concatBytes(
      new Uint8Array(signingPublicKey),
      new Uint8Array(encryptionPublicKey)
    )
  );
  const cryptoId = crypto.randomUUID();
  const privateKeyBundle = JSON.stringify({
    cryptoId,
    encryptionPrivateKeyPkcs8Base64: toBase64(encryptionPrivateKey),
    format: privateKeyBundleFormat,
    signingPrivateKeyPkcs8Base64: toBase64(signingPrivateKey)
  });

  return {
    cryptoKey: {
      algorithmSuite: "P-256-ECDSA/P-256-ECDH",
      cryptoId,
      encryptionPublicKeyBase64: toBase64(encryptionPublicKey),
      publicKeyFingerprint,
      publicKeyFormat: "spki",
      signingPublicKeyBase64: toBase64(signingPublicKey)
    },
    privateKeyBundle,
    privateKeyBundleFormat,
    publicKeyFingerprint
  };
}

function createRecoveryPhrase() {
  return generateMnemonic(wordlist, 128);
}

async function wrapPrivateKeyBundle(
  wrapKind: "password" | "recovery_phrase",
  secret: string,
  privateKeyBundle: string,
  privateKeyBundleFormat: string,
  localSecretFingerprint: string | null
) {
  const salt = getRandomBytes(16);
  const iv = getRandomBytes(12);
  const keyMaterial = await crypto.subtle.importKey(
    "raw",
    toArrayBuffer(textEncoder.encode(secret)),
    "PBKDF2",
    false,
    ["deriveKey"]
  );
  const key = await crypto.subtle.deriveKey(
    {
      hash: "SHA-256",
      iterations: 310000,
      name: "PBKDF2",
      salt: toArrayBuffer(salt)
    },
    keyMaterial,
    {
      length: 256,
      name: "AES-GCM"
    },
    false,
    ["encrypt"]
  );
  const ciphertext = await crypto.subtle.encrypt(
    {
      iv: toArrayBuffer(iv),
      name: "AES-GCM"
    },
    key,
    toArrayBuffer(textEncoder.encode(privateKeyBundle))
  );

  return {
    ciphertextBase64: toBase64(ciphertext),
    envelopeJson: JSON.stringify({
      algorithm: "AES-256-GCM",
      ivBase64: toBase64(iv),
      kdf: {
        hash: "SHA-256",
        iterations: 310000,
        name: "PBKDF2",
        saltBase64: toBase64(salt)
      },
      privateKeyBundleFormat,
      version: 1,
      wrapKind
    }),
    localSecretFingerprint,
    recipientCryptoId: null,
    wrapKind
  };
}

async function wrapPrivateKeyBundleWithPublicKey(
  recipient: PublicKeyWrapRecipient,
  privateKeyBundle: string,
  privateKeyBundleFormat: string
) {
  if (recipient.publicKeyFormat !== "spki") {
    throw new Error("Recipient public key format is not supported.");
  }

  const recipientPublicKey = await crypto.subtle.importKey(
    "spki",
    toArrayBuffer(fromBase64(recipient.encryptionPublicKeyBase64)),
    {
      name: "ECDH",
      namedCurve: "P-256"
    },
    false,
    []
  );
  const ephemeralKeyPair = await crypto.subtle.generateKey(
    {
      name: "ECDH",
      namedCurve: "P-256"
    },
    true,
    ["deriveBits"]
  );
  const sharedSecret = await crypto.subtle.deriveBits(
    {
      name: "ECDH",
      public: recipientPublicKey
    },
    ephemeralKeyPair.privateKey,
    256
  );
  const ephemeralPublicKey = await crypto.subtle.exportKey(
    "spki",
    ephemeralKeyPair.publicKey
  );
  const salt = getRandomBytes(16);
  const iv = getRandomBytes(12);
  const info = textEncoder.encode(
    `encryptedworkspace.public-key-wrap.v1:${recipient.cryptoId}:${privateKeyBundleFormat}`
  );
  const keyMaterial = await crypto.subtle.importKey(
    "raw",
    sharedSecret,
    "HKDF",
    false,
    ["deriveKey"]
  );
  const key = await crypto.subtle.deriveKey(
    {
      hash: "SHA-256",
      info: toArrayBuffer(info),
      name: "HKDF",
      salt: toArrayBuffer(salt)
    },
    keyMaterial,
    {
      length: 256,
      name: "AES-GCM"
    },
    false,
    ["encrypt"]
  );
  const ciphertext = await crypto.subtle.encrypt(
    {
      iv: toArrayBuffer(iv),
      name: "AES-GCM"
    },
    key,
    toArrayBuffer(textEncoder.encode(privateKeyBundle))
  );

  return {
    ciphertextBase64: toBase64(ciphertext),
    envelopeJson: JSON.stringify({
      algorithm: "ECDH-P256-HKDF-SHA256-AES-256-GCM",
      ephemeralPublicKeyBase64: toBase64(ephemeralPublicKey),
      ivBase64: toBase64(iv),
      kdf: {
        hash: "SHA-256",
        infoBase64: toBase64(info),
        name: "HKDF",
        saltBase64: toBase64(salt)
      },
      privateKeyBundleFormat,
      recipientCryptoId: recipient.cryptoId,
      recipientPublicKeyFingerprint: recipient.publicKeyFingerprint,
      version: 1,
      wrapKind: "public_key"
    }),
    localSecretFingerprint: null,
    recipientCryptoId: recipient.cryptoId,
    wrapKind: "public_key" as const
  };
}

async function wrapPrivateKeyBundleWithRawKey(
  wrapKind: "password",
  keyBase64: string,
  privateKeyBundle: string,
  privateKeyBundleFormat: string,
  localSecretFingerprint: string | null
) {
  const iv = getRandomBytes(12);
  const key = await crypto.subtle.importKey(
    "raw",
    toArrayBuffer(fromBase64(keyBase64)),
    "AES-GCM",
    false,
    ["encrypt"]
  );
  const ciphertext = await crypto.subtle.encrypt(
    {
      iv: toArrayBuffer(iv),
      name: "AES-GCM"
    },
    key,
    toArrayBuffer(textEncoder.encode(privateKeyBundle))
  );

  return {
    ciphertextBase64: toBase64(ciphertext),
    envelopeJson: JSON.stringify({
      algorithm: "AES-256-GCM",
      ivBase64: toBase64(iv),
      kdf: {
        name: "OPAQUE-EXPORT-KEY",
        profile: "ENCRYPTEDWORKSPACE-OPAQUE-P256-SHA256-V1"
      },
      privateKeyBundleFormat,
      version: 1,
      wrapKind
    }),
    localSecretFingerprint,
    recipientCryptoId: null,
    wrapKind
  };
}

async function createFingerprint(bytes: Uint8Array) {
  const digest = await crypto.subtle.digest("SHA-256", toArrayBuffer(bytes));

  return Array.from(new Uint8Array(digest))
    .slice(0, 16)
    .map(byte => byte.toString(16).padStart(2, "0"))
    .join(":");
}

function concatBytes(left: Uint8Array, right: Uint8Array) {
  const result = new Uint8Array(left.length + right.length);

  result.set(left);
  result.set(right, left.length);

  return result;
}

function getRandomBytes(length: number) {
  const bytes = new Uint8Array(length);

  crypto.getRandomValues(bytes);

  return bytes;
}

function toArrayBuffer(bytes: Uint8Array): ArrayBuffer {
  return new Uint8Array(bytes).buffer as ArrayBuffer;
}

function toBase64(data: ArrayBuffer | Uint8Array) {
  const bytes = data instanceof Uint8Array ? data : new Uint8Array(data);
  let binary = "";

  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }

  return btoa(binary);
}

function fromBase64(value: string) {
  const binary = atob(value);
  const bytes = new Uint8Array(binary.length);

  for (let index = 0; index < binary.length; index++) {
    bytes[index] = binary.charCodeAt(index);
  }

  return bytes;
}

function toBase32(bytes: Uint8Array) {
  const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
  let output = "";
  let bits = 0;
  let bitCount = 0;

  for (const byte of bytes) {
    bits = (bits << 8) | byte;
    bitCount += 8;

    while (bitCount >= 5) {
      output += alphabet[(bits >> (bitCount - 5)) & 31];
      bitCount -= 5;
    }
  }

  if (bitCount > 0) {
    output += alphabet[(bits << (5 - bitCount)) & 31];
  }

  return output;
}
