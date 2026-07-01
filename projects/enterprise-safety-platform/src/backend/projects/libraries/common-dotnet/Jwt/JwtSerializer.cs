using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Common.Configuration;
using Platform.Common.Encoding;

namespace Platform.Common.Jwt;

/// <summary>
/// JWT Serializer for Serializing and Deserializing JWTs, both encrypted, and signed.
/// </summary>
public class JwtSerializer
{
	private readonly IJsonSerializer serializer = new JwtJsonSerializer();
	private readonly ILogger<JwtSerializer> logger;
	private readonly ValidationParameters validationParameters;
	private readonly List<JwtPreparedBuilder> preparedBuilders = [];
	private readonly JwtPreparedBuilder? signingBuilder;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="configuration"></param>
	/// <param name="logger"></param>
	/// <exception cref="InvalidOperationException"></exception>
	/// <exception cref="NotImplementedException"></exception>
	/// <exception cref="NotSupportedException"></exception>
	/// <exception cref="FormatException"></exception>
	public JwtSerializer(IConfiguration configuration, ILogger<JwtSerializer> logger)
	{
		this.logger = logger;

		logger.LogDebug("Setting up JwtSerializer");

		AuthSettings? authSettings = configuration
			.GetSection(AuthSettings.CONFIGURATION_KEY)
			.Get<AuthSettings>();

		if (authSettings is null)
		{
			throw new InvalidOperationException($"{AuthSettings.CONFIGURATION_KEY} must be configured for JWT serialization!");
		}
		
		this.validationParameters = new ValidationParameters
		{
			ValidateSignature = true,
			ValidateExpirationTime = false,
			ValidateIssuedTime = false,
			TimeMargin = authSettings.ClockSkewGraceSeconds ?? 60,
		};
		
		// Build the known keys and their algorithms
		foreach (KeyValuePair<string, string> keyAlgorithmEntry in authSettings.KeyAlgorithms ?? [])
		{
			string keyId = keyAlgorithmEntry.Key.ToLowerInvariant();
			string algorithm = keyAlgorithmEntry.Value.ToLowerInvariant();
			
			JwtAlgorithmTypes algType = algorithm switch
			{
				"http://www.w3.org/2001/04/xmldsig-more#hmac-sha256" or
					"hs256" or
					"hs384" or
					"hs512" => JwtAlgorithmTypes.HMAC,
				"rs256" or 
					"rs384" or 
					"rs512" => JwtAlgorithmTypes.RSA,
				"es256" or 
					"es384" or 
					"es512" => JwtAlgorithmTypes.ECDSA,
				_ => throw new NotImplementedException($"Unsupported JWT Algorithm: {keyId} = {algorithm}"),
			};
			
			int keyBitSize = algorithm switch
			{
				"http://www.w3.org/2001/04/xmldsig-more#hmac-sha256" or
					"hs256" or
					"rs256" or
					"es256" => 256,
				"hs384" or
					"rs384" or
					"es384" => 384,
				"hs512" or
					"rs512" or
					"es512" => 512,
				_ => throw new NotImplementedException($"Unsupported JWT Algorithm: {keyId} = {algorithm}"),
			};
			
			string? symmetricKey = null;
			RSA? rsaPrivateKey = null;
			RSA? rsaPublicKey = null;
			ECDsa? ecdsaPrivateKey = null;
			ECDsa? ecdsaPublicKey = null;
			
			if (algType == JwtAlgorithmTypes.HMAC)
			{
				if (authSettings.SymmetricKeys is null)
				{
					throw new NotSupportedException("No symmetric keys have been configured");
				}

				if (!authSettings.SymmetricKeys.TryGetValue(
					key: keyId, 
					value: out string[]? symmetricKeyParts))
				{
					throw new KeyNotFoundException($"The specified symmetric key was not configured: {keyId}");
				}

				symmetricKey = string.Join(
					separator: string.Empty, 
					value: symmetricKeyParts);

				if (symmetricKey.Length < keyBitSize / 8)
				{
					throw new FormatException(
						$"The specified symmetric key is too short to use with that algorithm: {keyId}");
				}
			}
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			// ReSharper disable once MergeIntoLogicalPattern
			else if (algType == JwtAlgorithmTypes.RSA || algType == JwtAlgorithmTypes.ECDSA)
			{
				if (authSettings.PrivateKeys is null)
				{
					throw new NotSupportedException("No Private Keys have been configured");
				}

				if (authSettings.PublicKeys is null)
				{
					throw new NotSupportedException("No Public Keys have been configured");
				}

				if (!authSettings.PrivateKeys.TryGetValue(
					key: keyId,
					value: out string[]? privateKeyParts))
				{
					throw new KeyNotFoundException($"The specified private key was not configured: {keyId}");
				}
				
				string privateKeyPem = string.Join(
					separator: string.Empty, 
					value: privateKeyParts);
				
				if (!authSettings.PublicKeys.TryGetValue(
					key: keyId,
					value: out string[]? publicKeyParts))
				{
					throw new KeyNotFoundException($"The specified public key was not configured: {keyId}");
				}
				
				string publicKeyPem = string.Join(
					separator: string.Empty, 
					value: publicKeyParts);

				if (algType == JwtAlgorithmTypes.RSA)
				{
					try
					{
						rsaPrivateKey = RSA.Create(keyBitSize);
						rsaPrivateKey.ImportFromPem(privateKeyPem.AsSpan());
					}
					catch (Exception ex)
					{
						throw new FormatException(
							message: $"The specified private key was not in the correct Base64UrlEncoded PEM format, see the inner exception: {keyId}",
							innerException: ex);
					}

					try
					{
						rsaPublicKey = RSA.Create(keyBitSize);
						rsaPublicKey.ImportFromPem(publicKeyPem.AsSpan());
					}
					catch (Exception ex)
					{
						throw new FormatException(
							message: $"The specified public key was not in the correct Base64UrlEncoded PEM format, see the inner exception: {keyId}",
							innerException: ex);
					}
					
				} 
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				else if (algType == JwtAlgorithmTypes.ECDSA)
				{
					ECCurve curve = keyBitSize switch
					{
						256 => ECCurve.NamedCurves.nistP256,
						384 => ECCurve.NamedCurves.nistP384,
						512 => ECCurve.NamedCurves.nistP521, // NOTE THE p-521 used for es512 mismatch
						_ => throw new NotSupportedException($"Unsupported key size for ECDSA: {keyBitSize}"),
					};
					
					try
					{
						ecdsaPrivateKey = ECDsa.Create(curve);
						ecdsaPrivateKey.ImportFromPem(privateKeyPem.AsSpan());
					}
					catch (Exception ex)
					{
						throw new FormatException(
							message: $"The specified private key was not in the correct Base64UrlEncoded PEM format, see the inner exception: {keyId}",
							innerException: ex);
					}

					try
					{
						ecdsaPublicKey = ECDsa.Create(curve);
						ecdsaPublicKey.ImportFromPem(publicKeyPem.AsSpan());
					}
					catch (Exception ex)
					{
						throw new FormatException(
							message: $"The specified public key was not in the correct Base64UrlEncoded PEM format, see the inner exception: {keyId}",
							innerException: ex);
					}
				}
			}
			
			IJwtAlgorithm builderAlgorithm = algorithm switch
			{
				"http://www.w3.org/2001/04/xmldsig-more#hmac-sha256" or
					"hs256" => new HMACSHA256Algorithm(),
				"hs384" => new HMACSHA384Algorithm(),
				"hs512" => new HMACSHA512Algorithm(),
				"rs256" => new RS256Algorithm(publicKey: rsaPublicKey, privateKey: rsaPrivateKey),
				"rs384" => new RS384Algorithm(publicKey: rsaPublicKey, privateKey: rsaPrivateKey),
				"rs512" => new RS512Algorithm(publicKey: rsaPublicKey, privateKey: rsaPrivateKey),
				"es256" => new ES256Algorithm(publicKey: ecdsaPublicKey, privateKey: ecdsaPrivateKey),
				"es384" => new ES384Algorithm(publicKey: ecdsaPublicKey, privateKey: ecdsaPrivateKey),
				"es512" => new ES512Algorithm(publicKey: ecdsaPublicKey, privateKey: ecdsaPrivateKey),
				_ => throw new NotImplementedException($"Unsupported JWT Algorithm: {keyId} = {algorithm}"),
			};
			
			JwtBuilder builder = JwtBuilder.Create()
				.WithJsonSerializer(serializer)
				.WithAlgorithm(builderAlgorithm)
				.AddHeader(name: "kid", value: keyId);

			if (algType == JwtAlgorithmTypes.HMAC)
			{
				builder = builder.WithSecret(symmetricKey);
			}

			preparedBuilders.Add(new JwtPreparedBuilder
			{
				KeyId = keyId,
				Algorithm = algorithm,
				Builder = builder,
			});
		}
		
#pragma warning disable CA1873
		logger.LogInformation(message: "Configured {numKeys} Jwt Keys", preparedBuilders.Count);
#pragma warning restore CA1873
		
		if (authSettings.SigningKeyId is null)
		{
			logger.LogWarning("No SigningKeyId has been configured. Will not be able to encode new JWTs");

			return;
		}

		signingBuilder = preparedBuilders
			.Where(x => x.KeyId == authSettings.SigningKeyId)
			.FirstOrDefault();

		if (signingBuilder is null)
		{
			throw new KeyNotFoundException("The specified Signing Key was not found after building the known keys!");
		}
	}

	/// <summary>
	/// Serialize an object as a JWT and sign it using the configured signing builder
	/// </summary>
	/// <param name="obj"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public string Serialize<T>(T obj) where T : class
	{
		if (signingBuilder is null)
		{
			throw new NotSupportedException("No SigningKeyId has been configured. Cannot encode new JWTs!");
		}
		
		return signingBuilder.Builder.Encode(obj);
	}

	/// <summary>
	/// Serialize an object as a JWT and sign it using a specific keyId
	/// </summary>
	/// <param name="obj"></param>
	/// <param name="keyId"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public string Serialize<T>(T obj, string keyId) where T : class
	{
		JwtPreparedBuilder? builder = preparedBuilders
			.Where(x => x.KeyId == keyId)
			.FirstOrDefault();
		
		if (builder is null)
		{
			throw new KeyNotFoundException("The requested JWT key was not available!");
		}
		
		return builder.Builder.Encode(obj);
	}

	/// <summary>
	/// Deserialize an object from a JWT and check signature.
	/// </summary>
	/// <param name="jwt"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public T Deserialize<T>(string jwt) where T : class
	{
		if (preparedBuilders.Count == 0)
		{
			throw new NotSupportedException("No Signing Keys have been configured");
		}
		
		JwtHeader header = JwtBuilder.Create()
			.WithJsonSerializer(serializer)
			.DecodeHeader<JwtHeader>(jwt);
		
		if (header.KeyId is null)
		{
			throw new KeyNotFoundException("The token does not have a Key Id in its header!");
		}

		JwtPreparedBuilder? builder = preparedBuilders
			.Where(x => x.KeyId.Equals(
				value: header.KeyId, 
				comparisonType: StringComparison.InvariantCultureIgnoreCase))
			.FirstOrDefault();

		if (builder is null)
		{
			throw new KeyNotFoundException("The key used to encode this JWT is not configured in this application!");
		}

		if (!builder.Algorithm.Equals(
			value: header.Algorithm, 
			comparisonType: StringComparison.InvariantCultureIgnoreCase))
		{
			throw new FormatException(
				"The key used to encode this JWT is known, however the algorithm used in the JWT does not match the algorithm configured in this application!");
		}

		return builder.Builder
			.WithValidationParameters(validationParameters)
			.Decode<T>(jwt);
	}
}