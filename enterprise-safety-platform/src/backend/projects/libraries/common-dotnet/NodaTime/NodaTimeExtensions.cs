using NodaTime;

namespace Platform.Common.NodaTime;

/// <summary>
/// Extension methods for NodaTime
/// </summary>
public static class NodaTimeExtensions
{
	/// <summary>
	/// Determine if the Instant occured prior to (or equal to) the current SystemClock's Instant
	/// </summary>
	/// <param name="instant"></param>
	/// <returns></returns>
	public static bool HasExpired(this Instant instant)
	{
		return instant <= SystemClock.Instance.GetCurrentInstant();
	}
}