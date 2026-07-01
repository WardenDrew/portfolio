namespace Platform.Legacy.Common.Models;

/// <summary>
/// The shared error code model for the legacy api
/// </summary>
public interface IErrorCode
{
	/// <summary>
	/// The code
	/// </summary>
	string Code { get; }

	/// <summary>
	/// The english translation to provide if no translation was found on the client side
	/// </summary>
	string EnglishTranslation { get; }

	/// <summary>
	/// The HTTP Status Code to return
	/// </summary>
	int? HTTPStatusCode { get; }
}
