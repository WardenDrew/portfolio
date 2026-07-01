namespace Platform.Legacy.Core.Models.API;

public interface IResponseMeta
{
	/// <summary>
	/// Number of items returned in this response
	/// </summary>
	int? NumReturned { get; set; }

	/// <summary>
	/// Number of items skipped in this response
	/// </summary>
	int? NumSkipped { get; set; }

	/// <summary>
	/// Number of items total on the server
	/// </summary>
	int? NumTotal { get; set; }

	/// <summary>
	/// Number of items remaining on the server that have not been returned
	/// </summary>
	int? NumRemaining { get; }

	/// <summary>
	/// Are there no more items remaining
	/// </summary>
	bool? EndOfResults { get; }

	/// <summary>
	/// Debug information to kick back to the frontend
	/// </summary>
	string? Debug { get; set; }
}
