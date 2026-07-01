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
public sealed class JsonDoNotSerializeAttribute : JsonAttribute;

/// <summary>
/// 
/// </summary>
public static class JsonDoNotSerializeExtensions
{
	/// <summary>
	/// 
	/// </summary>
	public static Action<JsonTypeInfo> ApplyJsonDoNotSerializeAttribute { get; } = static typeInfo =>
	{
		if (typeInfo.Kind != JsonTypeInfoKind.Object)
		{
			return;
		}

		foreach (JsonPropertyInfo? property in typeInfo.Properties)
		{
			if (property.AttributeProvider?
				.GetCustomAttributes(attributeType: typeof(JsonDoNotSerializeAttribute), inherit: true)
				.Length != 0)
			{
				property.ShouldSerialize = JsonDoNotSerializeExtensions.returnFalse;
			}
		}
	};

	private static readonly Func<object?,object?,bool> returnFalse = static (_, _) => false;
}