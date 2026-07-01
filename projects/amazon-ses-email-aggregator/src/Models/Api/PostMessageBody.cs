namespace SESAggregator.Models.Api;

public class PostMessageBody
{
	public required string Address { get; set; }
	public required string Name { get; set; }
	public required string Subject { get; set; }
	public required string Body { get; set; }
}
