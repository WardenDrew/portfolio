using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Primitives;
using Platform.Common.Jwt;

namespace Platform.Legacy.Api.Services;

/// <summary>
/// Service to determine if a given feature flag is set
/// </summary>
public class FeatureFlagService
{
	/// <summary>
	/// Flags that were set in the header
	/// </summary>
	public List<string> Flags { get; private set; } = [];

	private readonly JwtSerializer jwtSerializer;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="httpContextAccessor"></param>
	/// <param name="jwtSerializer"></param>
	/// <exception cref="InvalidOperationException"></exception>
	public FeatureFlagService(IHttpContextAccessor httpContextAccessor, JwtSerializer jwtSerializer)
	{
		this.jwtSerializer = jwtSerializer;
		HttpContext? context = httpContextAccessor.HttpContext;

		if (context is null || !context.Request.Headers.TryGetValue(key: "X-FEATURE-FLAGS", value: out StringValues values))
		{
			// Not in a request context or header not set
			return;
		}

		FeatureFlagsModel model = jwtSerializer.Deserialize<FeatureFlagsModel>(values.ToString());

		Flags = model.Flags;
	}

	/// <summary>
	/// Check if a flag is set
	/// </summary>
	/// <param name="flag"></param>
	/// <returns></returns>
	public bool Check(string flag)
	{
		return Flags.Where(x => x.Equals(value: flag, comparisonType: StringComparison.InvariantCultureIgnoreCase))
			.Any();
	}

	/// <summary>
	/// Build a feature flags JWT from a list of flags
	/// </summary>
	/// <param name="flags"></param>
	/// <returns></returns>
	public string BuildFlags(List<string> flags)
	{
		return jwtSerializer.Serialize(new FeatureFlagsModel() { Flags = flags, });
	}

	/// <summary>
	/// POCO Model for Feature Flags
	/// </summary>
	public class FeatureFlagsModel
	{
		/// <summary>
		/// The Flags
		/// </summary>
		public List<string> Flags { get; set; } = [];
	}
}
