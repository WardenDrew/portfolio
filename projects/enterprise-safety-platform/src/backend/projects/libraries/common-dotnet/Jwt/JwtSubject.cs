using System;

namespace Platform.Common.Jwt;

/// <summary>
/// 
/// </summary>
public class JwtSubject
{
	/// <summary>
	/// Id of the subject
	/// </summary>
	public required string Id { get; init; }
	
	/// <summary>
	/// Source the subject can be authenticated against
	/// </summary>
	public string? Source { get; init; }

	/// <summary>
	/// Source|Id formatted
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return $"{Source}{(Source != null ? "|" : "")}{Id}";
	}
	
	/// <summary>
	/// The source name for our internal subjects
	/// </summary>
	public static readonly string INTERNAL_SUBJECT_SOURCE = "platform";

	/// <summary>
	/// Attempts to parse the subject as an internal subject. Returns null if it fails
	/// </summary>
	/// <returns></returns>
	public int? ToInternal()
	{
		if (this.Source is null ||
			!this.Source.Equals(value: JwtSubject.INTERNAL_SUBJECT_SOURCE, comparisonType: StringComparison.OrdinalIgnoreCase) ||
			!int.TryParse(s: this.Id, result: out int userId))
		{
			return null;
		}

		return userId;
	}

	/// <summary>
	/// Creates a new Internal subject definition
	/// </summary>
	/// <param name="UserId"></param>
	/// <returns></returns>
	public static JwtSubject FromInternal(int UserId)
	{
		return new JwtSubject()
		{
			Id = UserId.ToString(),
			Source = JwtSubject.INTERNAL_SUBJECT_SOURCE,
		};
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="subject"></param>
	/// <returns></returns>
	public static JwtSubject? Parse(string? subject)
	{
		if (subject is null) return null;
		
		string[] subParts = subject.Split(separator: '|', count: 2);
		if (subParts.Length == 2)
		{
			return new JwtSubject()
			{
				Id = subParts[1],
				Source = subParts[0],
			};
		}
		else if (subParts.Length == 1)
		{
			return new JwtSubject()
			{
				Id = subParts[0],
			};
		}

		return null;
	}
}