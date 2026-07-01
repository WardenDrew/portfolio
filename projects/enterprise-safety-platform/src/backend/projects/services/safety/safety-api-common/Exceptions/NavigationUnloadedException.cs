namespace Platform.Legacy.Common.Exceptions;

/// <summary>
/// Thrown when accessing an EF navigation that was not loaded yet
/// </summary>
public class NavigationUnloadedException : Exception
{
	/// <inheritdoc />
	public NavigationUnloadedException() { }

	/// <inheritdoc />
	public NavigationUnloadedException(string? message)
		: base(message) { }
}
