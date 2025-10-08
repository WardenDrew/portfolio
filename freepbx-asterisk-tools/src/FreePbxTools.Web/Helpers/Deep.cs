using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace FreePbxTools.Web.Helpers;

public static class Deep
{
	[return: NotNullIfNotNull(nameof(model))]
	public static T? Copy<T>(T? model) where T : class
	{
		if (model is null) return null;
		
		T? copy = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(model));
		if (copy is null)
		{
			throw new InvalidOperationException("Deep copy failed!");
		}

		return copy;
	}


}