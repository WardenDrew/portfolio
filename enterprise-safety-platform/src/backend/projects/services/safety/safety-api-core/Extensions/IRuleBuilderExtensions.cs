namespace Platform.Legacy.Core.Extensions;

public static class IRuleBuilderExtensions
{
	public static IRuleBuilderOptions<T, string?> PhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
	{
		return ruleBuilder
			.Must(x => x.IsE164PhoneNumber())
			.WithMessage("The phone number must be a validly formatted E.164 number!");
	}

	public static IRuleBuilderOptions<T, string?> LatLon<T>(this IRuleBuilder<T, string?> ruleBuilder)
	{
		return ruleBuilder
			.Must(x => x.IsGeolocationPattern())
			.WithMessage("The coordinate pair must be a validly formatted coordinates with optional accuracy!");
	}

	/// <summary>
	/// Only checks if string is whitespace, if its null the rule passes
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="ruleBuilder"></param>
	/// <returns></returns>
	public static IRuleBuilderOptions<T, string?> NotWhiteSpace<T>(this IRuleBuilder<T, string?> ruleBuilder)
	{
		return ruleBuilder
			.Must(x => x is null || x.Length == 0 || !string.IsNullOrWhiteSpace(x))
			.WithMessage("The string has non-zero length but only consists of whitespace characters!");
	}
}
