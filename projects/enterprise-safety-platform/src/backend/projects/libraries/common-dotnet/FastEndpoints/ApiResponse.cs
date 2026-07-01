using System.Collections.Generic;
using System.Text.Json.Serialization;
using Platform.Common.Models;

namespace Platform.Common.FastEndpoints;

/// <summary>
/// 
/// </summary>
public class ApiResponse(string? type) : TypedModel(type)
{
	/// <summary>
	/// Meta dictionary
	/// </summary>
	[JsonPropertyName("$meta")]
	public Dictionary<string, string>? Meta { get; set; } = [];
}
	
	
