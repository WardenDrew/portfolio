namespace Platform.Legacy.Common.Models;

/// <inheritdoc />
public class ErrorCode : IErrorCode
{
	/// <inheritdoc />
	public string Code { get; }

	/// <inheritdoc />
	public string EnglishTranslation { get; }

	/// <inheritdoc />
	public int? HTTPStatusCode { get; }

	/// <summary>
	/// Create an error code object
	/// </summary>
	/// <param name="name"></param>
	/// <param name="section"></param>
	/// <param name="englishTranslation"></param>
	/// <param name="httpStatusCode"></param>
	public ErrorCode(string name, string section, string englishTranslation, int? httpStatusCode = null)
	{
		this.Code = ErrorCode.NameToCode(name: name, section: section);
		this.EnglishTranslation = englishTranslation;
		this.HTTPStatusCode = httpStatusCode;
	}

	private static string NameToCode(string name, string section)
	{
		string code = "err_";
		code += section.ToLower();
		code += "_";

		string[] nameSegments = name.Split('_');
		bool firstSegment = true;
		foreach (string segment in nameSegments)
		{
			if (firstSegment)
			{
				firstSegment = false;
				code += segment.ToLower();
			}
			else
			{
				code += string.Concat(str0: segment[0].ToString().ToUpper(), str1: segment.ToLower().AsSpan(1));
			}
		}

		return code;
	}

	/// <summary>
	/// The default support message
	/// </summary>
	public static readonly string SUPPORT = "Please contact the support team.";
}
