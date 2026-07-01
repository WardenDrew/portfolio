namespace SESAggregator.Models;

public class SendMessageResponse
{
	public bool Success { get; set; }
	public bool Retryable { get; set; }
	public bool Backoff { get; set; }
	public string? FailureMessage { get; set; }
}
