namespace Platform.Legacy.Core.Models;

public class PagingParameters
{
	public string OrderByField { get; set; } = "Id";
	public bool Ascending { get; set; } = true;
	public int SkipNum { get; set; } = 0;
	public int TakeNum { get; set; } = 10;
}
