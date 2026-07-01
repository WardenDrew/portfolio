using System;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using JWT;
using Platform.Common.Json;

namespace Platform.Common.Jwt;

/// <summary>
/// System.Text.Json Serializer used by JwtSerializer that automatically converts NodaTime.Instant to Unix Time seconds
/// </summary>
public class JwtJsonSerializer : IJsonSerializer
{
	private readonly JsonSerializerOptions options;

	/// <summary>
	/// Default Constructor
	/// </summary>
	public JwtJsonSerializer()
	{
		this.options = new JsonSerializerOptions
		{
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
		};
		this.options.Converters.Add(new NodaTimeInstantConverter());
		this.options.Converters.Add(new DateTimeConverter());
		this.options.TypeInfoResolver = 
			(this.options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
			.WithAddedModifier(JsonDoNotSerializeExtensions.ApplyJsonDoNotSerializeAttribute)
			.WithAddedModifier(JsonNotRequiredExtensions.ApplyJsonNotRequiredAttribute);
	}
        
	/// <summary>
	/// Serialize an object
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public string Serialize(object obj)
	{
		return JsonSerializer.Serialize(value: obj, options: this.options);
	}
    
	/// <summary>
	/// Deserialize to an object
	/// </summary>
	/// <param name="type"></param>
	/// <param name="json"></param>
	/// <returns></returns>
	/// <exception cref="SerializationException"></exception>
	public object Deserialize(Type type, string json)
	{
		return JsonSerializer.Deserialize(json: json, returnType: type, options: this.options)
			?? throw new SerializationException("Could not deserialize Jwt JSON");
	}
}