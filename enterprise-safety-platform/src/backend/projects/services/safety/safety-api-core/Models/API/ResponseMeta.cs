namespace Platform.Legacy.Core.Models.API;

public class ResponseMeta : IResponseMeta
{
	/// <summary>
	/// Number of items returned in this response
	/// </summary>
	public int? NumReturned { get; set; }

	/// <summary>
	/// Number of items skipped in this response
	/// </summary>
	public int? NumSkipped { get; set; }

	/// <summary>
	/// Number of items total on the server
	/// </summary>
	public int? NumTotal { get; set; }

	/// <summary>
	/// Number of items remaining on the server that have not been returned
	/// </summary>
	public int? NumRemaining
	{
		get
		{
			if (this.NumTotal is null || this.NumSkipped is null || this.NumReturned is null)
			{
				return null;
			}

			int? remain = this.NumTotal - this.NumSkipped - this.NumReturned;
			if (remain < 0)
			{
				remain = 0;
			}

			return remain;
		}
	}

	/// <summary>
	/// Are there no more items remaining
	/// </summary>
	public bool? EndOfResults => this.NumRemaining == 0;

	/// <summary>
	/// Debug information to kick back to the frontend
	/// </summary>
	public string? Debug { get; set; }
}
