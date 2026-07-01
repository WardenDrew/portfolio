using System;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Platform.Common.Json;

/// <summary>
/// 
/// </summary>
[System.AttributeUsage(
	System.AttributeTargets.Property | System.AttributeTargets.Field, 
	AllowMultiple = false)]
public sealed class JsonNotRequiredAttribute : JsonAttribute;

/// <summary>
/// 
/// </summary>
public static class JsonNotRequiredExtensions
{
	/// <summary>
	/// 
	/// </summary>
	public static Action<JsonTypeInfo> ApplyJsonNotRequiredAttribute { get; } = static typeInfo =>
	{
		if (typeInfo.Kind != JsonTypeInfoKind.Object)
		{
			return;
		}

		foreach (JsonPropertyInfo? property in typeInfo.Properties)
		{
			if (property.AttributeProvider?
				.GetCustomAttributes(attributeType: typeof(JsonNotRequiredAttribute), inherit: true)
				.Length != 0)
			{
				property.IsRequired = false;
			}
		}
	};
}