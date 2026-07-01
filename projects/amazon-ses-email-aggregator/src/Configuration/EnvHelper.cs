using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SESAggregator.Configuration;

public static class EnvHelper
{
	public static void LoadENVFile(string path)
	{
		if (!File.Exists(path))
		{
			throw new FileNotFoundException($"Could not find the environment variable file to load.\nPath: {path}\nWorking Directory: {Environment.CurrentDirectory}");
		}

		using StreamReader sr = new(path);

		while (!sr.EndOfStream)
		{
			string? line = sr.ReadLine();

			// null guard
			if (line is null)
			{
				continue;
			}

			// Stripe leadin and trailing whitespace
			line = line.Trim();

			// need at least 3 symbols on the line for valid a=b format
			if (line.Length <= 2)
			{
				continue;
			}

			// ignore lines that start with a comment
			if (line.StartsWith("#"))
			{
				continue;
			}

			// split string into max 2 parts
			string[] parts = line.Split(new[] { '=' }, 2);

			// ignore lines with no value side
			if (parts.Length != 2)
			{
				continue;
			}

			// Ignore whitespace variables
			if (string.IsNullOrWhiteSpace(parts[0]) ||
				string.IsNullOrWhiteSpace(parts[1]))
			{
				continue;
			}

			// Set the ENV variable
			Environment.SetEnvironmentVariable(parts[0], parts[1]);
		}
	}

	public static bool Validate(out List<string> missingEnvKeys, params Assembly[] assemblies)
	{
		List<Type> envSettingsTypes = new();
		List<string> allEnvKeys = new();
		missingEnvKeys = new();
		
		foreach (Assembly assembly in assemblies)
		{
			envSettingsTypes.AddRange(assembly.GetTypes()
				.Where(x => typeof(IEnvSettings).IsAssignableFrom(x) && !x.IsAbstract));
		}

		foreach (Type settingsType in envSettingsTypes)
		{
			allEnvKeys.AddRange(settingsType
				.GetProperties(
					BindingFlags.Public |
					BindingFlags.Static |
					BindingFlags.FlattenHierarchy)
				.Where(x => x.GetCustomAttribute<RequiredAttribute>() != null)
				.Select(x => x.Name));
		}

		foreach (string key in allEnvKeys)
		{
			string? value = Environment.GetEnvironmentVariable(key);

			if (string.IsNullOrWhiteSpace(value))
			{
				missingEnvKeys.Add(key);
			}
		}

		return !missingEnvKeys.Any();
	}
}
