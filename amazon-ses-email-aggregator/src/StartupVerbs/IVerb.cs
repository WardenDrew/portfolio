namespace SESAggregator.StartupVerbs;

public interface IVerb
{
	public Task<int> Handle(string[] args);
}
