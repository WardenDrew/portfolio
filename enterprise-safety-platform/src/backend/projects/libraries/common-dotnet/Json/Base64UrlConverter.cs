using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Common.Encoding;

namespace Platform.Common.Json;

/// <summary>
/// JsonConverter type to convert existing strings to Base64 URL safe encoded strings
/// </summary>
public class Base64UrlConverter : JsonConverter<string>
{
	/// <summary>
	/// Overriden JsonConverter.Read(...)
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="typeToConvert"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string? incoming = reader.GetString();
		if (incoming is null) return null;
        
		return Base64Helper.UrlDecodeString(incoming);
	}
    
	/// <summary>
	/// Override JsonConverter.Write(...)
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="value"></param>
	/// <param name="options"></param>
	public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(Base64Helper.UrlEncodeString(value));
	}
}