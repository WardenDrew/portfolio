using CommandLine;
using SESAggregator.StartupVerbs;
using System.Reflection;

namespace SESAggregator.Extensions.CommandLineParser;

public static class CommandLineParserExtensions
{
	public static ParserResult<object> ScanAndParseVerbs(this Parser parser, string[] args, params Assembly[] assemblies)
	{
		List<Type> types = new();

		foreach (Assembly assembly in assemblies)
		{
			types.AddRange(assembly.GetTypes()
				.Where(x => typeof(IVerb).IsAssignableFrom(x))
				.Where(x => !x.IsInterface)
				.Where(x => !x.IsAbstract)
				.ToList());
		}

		return parser.ParseArguments(args, types.ToArray());
	}

	public static ParserResult<object> ScanAndParseVerbs(this Parser parser, string[] args, params Type[] assemblyMarkers)
	{
		List<Assembly> assemblies = new();

		foreach (Type assemblyMarker in assemblyMarkers)
		{
			assemblies.Add(assemblyMarker.Assembly);
		}

		return parser.ScanAndParseVerbs(args, assemblies.ToArray());
	}
}
