using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EncryptedWorkspace.Opaque;

public sealed class OpaqueServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(
        JsonSerializerDefaults.Web
    );

    private readonly IReadOnlyDictionary<string, OpaqueServerKey> keys;

    public OpaqueServer(IEnumerable<OpaqueServerKey> keys, string activeKeyId)
    {
        var preparedKeys = keys.ToDictionary(
            key => key.KeyId,
            StringComparer.Ordinal
        );

        if (preparedKeys.Count == 0)
        {
            throw new OpaqueProtocolException(
                "At least one OPAQUE server key is required."
            );
        }

        if (!preparedKeys.TryGetValue(activeKeyId, out var activeKey))
        {
            throw new OpaqueProtocolException(
                $"Active OPAQUE server key '{activeKeyId}' was not configured."
            );
        }

        this.keys = preparedKeys;
        ActiveKey = activeKey;
    }

    public OpaqueServerKey ActiveKey { get; }

    public OpaqueRegistrationStartResponse CreateRegistrationStart(
        string blindedElementBase64
    )
    {
        return new OpaqueRegistrationStartResponse(
            ActiveKey.KeyId,
            ActiveKey.PublicKeyBase64,
            EvaluateOprf(ActiveKey, blindedElementBase64)
        );
    }

    public OpaqueLoginStartResponse CreateLoginStart(
        string blindedElementBase64,
        string registrationRecordJson
    )
    {
        var record = ParseAndValidateRegistrationRecord(registrationRecordJson);
        var key = GetKey(record.ServerKeyId);
        var evaluatedElementBase64 = EvaluateOprf(key, blindedElementBase64);
        var serverEphemeralScalar = P256.RandomScalar();
        var serverEphemeralPublicKeyBase64 = P256.SerializePointBase64(
            P256.ScalarMultiply(serverEphemeralScalar, P256.Generator)
        );
        var serverNonceBase64 = OpaqueEncoding.ToBase64(
            OpaqueCrypto.RandomBytes(32)
        );
        var state = new OpaqueLoginState(
            key.KeyId,
            blindedElementBase64,
            evaluatedElementBase64,
            serverNonceBase64,
            P256.ToScalarBase64(serverEphemeralScalar),
            serverEphemeralPublicKeyBase64,
            OpaqueEncoding.ToBase64(
                HashRegistrationRecord(registrationRecordJson)
            )
        );

        return new OpaqueLoginStartResponse(
            key.KeyId,
            evaluatedElementBase64,
            record.ServerPublicKeyBase64,
            record.ClientPublicKeyBase64,
            record.Envelope.NonceBase64,
            record.Envelope.CiphertextBase64,
            serverNonceBase64,
            serverEphemeralPublicKeyBase64,
            state
        );
    }

    public void ValidateRegistrationRecord(string registrationRecordJson)
    {
        _ = ParseAndValidateRegistrationRecord(registrationRecordJson);
    }

    public bool VerifyLoginFinish(
        string registrationRecordJson,
        OpaqueLoginState state,
        string clientNonceBase64,
        string clientEphemeralPublicKeyBase64,
        string clientMacBase64
    )
    {
        var record = ParseAndValidateRegistrationRecord(registrationRecordJson);

        if (
            !OpaqueEncoding.FixedTimeEqualsBase64(
                OpaqueEncoding.ToBase64(
                    HashRegistrationRecord(registrationRecordJson)
                ),
                state.RegistrationRecordHashBase64
            )
        )
        {
            throw new OpaqueProtocolException(
                "OPAQUE login state does not match the stored registration record."
            );
        }

        var key = GetKey(state.ServerKeyId);
        EnsurePublicKeyMatches(key, record.ServerPublicKeyBase64);

        var clientPublicKey = P256.ParsePoint(
            record.ClientPublicKeyBase64,
            "OPAQUE client public key"
        );
        var clientEphemeralPublicKey = P256.ParsePoint(
            clientEphemeralPublicKeyBase64,
            "OPAQUE client ephemeral public key"
        );
        var serverEphemeralPrivateScalar = P256.ParseScalarBase64(
            state.ServerEphemeralPrivateKeyBase64,
            "OPAQUE server ephemeral private key"
        );
        var sharedSecret = ComputeServerSharedSecret(
            key,
            serverEphemeralPrivateScalar,
            clientPublicKey,
            clientEphemeralPublicKey
        );
        var transcriptHash = ComputeTranscriptHash(
            record,
            state,
            clientNonceBase64,
            clientEphemeralPublicKeyBase64
        );
        var sessionKey = OpaqueCrypto.HkdfSha256(
            sharedSecret,
            transcriptHash,
            OpaqueConstants.SessionLabel,
            32
        );
        var expectedClientMac = OpaqueCrypto.HmacSha256(
            sessionKey,
            OpaqueEncoding.Concat(
                OpaqueConstants.ClientMacLabel,
                transcriptHash
            )
        );
        var actualClientMac = OpaqueEncoding.FromBase64(
            clientMacBase64,
            "OPAQUE client MAC"
        );

        return actualClientMac.Length == expectedClientMac.Length
            && CryptographicOperations.FixedTimeEquals(
                actualClientMac,
                expectedClientMac
            );
    }

    public static string SerializeLoginState(OpaqueLoginState state)
    {
        return JsonSerializer.Serialize(state, JsonOptions);
    }

    public static OpaqueLoginState DeserializeLoginState(string stateJson)
    {
        try
        {
            return JsonSerializer.Deserialize<OpaqueLoginState>(
                    stateJson,
                    JsonOptions
                )
                ?? throw new OpaqueProtocolException(
                    "OPAQUE login state was empty."
                );
        }
        catch (JsonException exception)
        {
            throw new OpaqueProtocolException(
                $"OPAQUE login state was invalid: {exception.Message}"
            );
        }
    }

    private OpaqueRegistrationRecord ParseAndValidateRegistrationRecord(
        string registrationRecordJson
    )
    {
        if (string.IsNullOrWhiteSpace(registrationRecordJson))
        {
            throw new OpaqueProtocolException(
                "OPAQUE registration record is required."
            );
        }

        OpaqueRegistrationRecord record;

        try
        {
            record =
                JsonSerializer.Deserialize<OpaqueRegistrationRecord>(
                    registrationRecordJson,
                    JsonOptions
                )
                ?? throw new OpaqueProtocolException(
                    "OPAQUE registration record was empty."
                );
        }
        catch (JsonException exception)
        {
            throw new OpaqueProtocolException(
                $"OPAQUE registration record must be valid JSON: {exception.Message}"
            );
        }

        if (
            record.Version != OpaqueConstants.Version
            || !string.Equals(
                record.Profile,
                OpaqueConstants.Profile,
                StringComparison.Ordinal
            )
        )
        {
            throw new OpaqueProtocolException(
                "OPAQUE registration record profile is unsupported."
            );
        }

        var key = GetKey(record.ServerKeyId);
        EnsurePublicKeyMatches(key, record.ServerPublicKeyBase64);
        _ = P256.ParsePoint(
            record.ClientPublicKeyBase64,
            "OPAQUE client public key"
        );
        _ = OpaqueEncoding.FromBase64(
            record.Envelope.NonceBase64,
            "OPAQUE envelope nonce"
        );
        _ = OpaqueEncoding.FromBase64(
            record.Envelope.CiphertextBase64,
            "OPAQUE envelope ciphertext"
        );

        return record;
    }

    private OpaqueServerKey GetKey(string keyId)
    {
        if (!keys.TryGetValue(keyId, out var key))
        {
            throw new OpaqueProtocolException(
                $"OPAQUE server key '{keyId}' is not configured."
            );
        }

        return key;
    }

    private static string EvaluateOprf(
        OpaqueServerKey key,
        string blindedElementBase64
    )
    {
        var blindedElement = P256.ParsePoint(
            blindedElementBase64,
            "OPAQUE blinded element"
        );
        var evaluatedElement = P256.ScalarMultiply(
            key.PrivateScalar,
            blindedElement
        );

        if (evaluatedElement.IsInfinity)
        {
            throw new OpaqueProtocolException(
                "OPAQUE evaluated element was invalid."
            );
        }

        return P256.SerializePointBase64(evaluatedElement);
    }

    private static byte[] ComputeServerSharedSecret(
        OpaqueServerKey key,
        System.Numerics.BigInteger serverEphemeralPrivateScalar,
        P256Point clientPublicKey,
        P256Point clientEphemeralPublicKey
    )
    {
        var dh1 = P256.XCoordinate(
            P256.ScalarMultiply(key.PrivateScalar, clientPublicKey)
        );
        var dh2 = P256.XCoordinate(
            P256.ScalarMultiply(key.PrivateScalar, clientEphemeralPublicKey)
        );
        var dh3 = P256.XCoordinate(
            P256.ScalarMultiply(serverEphemeralPrivateScalar, clientPublicKey)
        );

        return OpaqueEncoding.Concat(dh1, dh2, dh3);
    }

    private static byte[] ComputeTranscriptHash(
        OpaqueRegistrationRecord record,
        OpaqueLoginState state,
        string clientNonceBase64,
        string clientEphemeralPublicKeyBase64
    )
    {
        return OpaqueCrypto.Sha256(
            OpaqueEncoding.ConcatLengthPrefixed(
                Encoding.UTF8.GetBytes(OpaqueConstants.Profile),
                OpaqueEncoding.FromBase64(
                    state.BlindedElementBase64,
                    "OPAQUE blinded element"
                ),
                OpaqueEncoding.FromBase64(
                    state.EvaluatedElementBase64,
                    "OPAQUE evaluated element"
                ),
                OpaqueEncoding.FromBase64(
                    record.ClientPublicKeyBase64,
                    "OPAQUE client public key"
                ),
                OpaqueEncoding.FromBase64(
                    record.ServerPublicKeyBase64,
                    "OPAQUE server public key"
                ),
                OpaqueEncoding.FromBase64(
                    clientEphemeralPublicKeyBase64,
                    "OPAQUE client ephemeral public key"
                ),
                OpaqueEncoding.FromBase64(
                    state.ServerEphemeralPublicKeyBase64,
                    "OPAQUE server ephemeral public key"
                ),
                OpaqueEncoding.FromBase64(
                    clientNonceBase64,
                    "OPAQUE client nonce"
                ),
                OpaqueEncoding.FromBase64(
                    state.ServerNonceBase64,
                    "OPAQUE server nonce"
                ),
                OpaqueEncoding.FromBase64(
                    record.Envelope.NonceBase64,
                    "OPAQUE envelope nonce"
                ),
                OpaqueEncoding.FromBase64(
                    record.Envelope.CiphertextBase64,
                    "OPAQUE envelope ciphertext"
                )
            )
        );
    }

    private static byte[] HashRegistrationRecord(string registrationRecordJson)
    {
        return OpaqueCrypto.Sha256(
            Encoding.UTF8.GetBytes(registrationRecordJson)
        );
    }

    private static void EnsurePublicKeyMatches(
        OpaqueServerKey key,
        string publicKeyBase64
    )
    {
        if (
            !OpaqueEncoding.FixedTimeEqualsBase64(
                key.PublicKeyBase64,
                publicKeyBase64
            )
        )
        {
            throw new OpaqueProtocolException(
                "OPAQUE registration record server key does not match configuration."
            );
        }
    }
}
