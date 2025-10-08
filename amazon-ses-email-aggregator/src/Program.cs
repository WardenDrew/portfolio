using CommandLine;
using SESAggregator;
using SESAggregator.Extensions.CommandLineParser;
using SESAggregator.StartupVerbs;

int exitCode = await Parser.Default.ScanAndParseVerbs(args, AssemblyMarker.Assembly)
	.MapResult(
	(IVerb verb) => verb.Handle(args),
	errors => Task.FromResult(-1));

return exitCode;
	