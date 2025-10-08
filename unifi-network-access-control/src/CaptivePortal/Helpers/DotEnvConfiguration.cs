using CaptivePortal.Services.Outer;
using Microsoft.Extensions.Primitives;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CaptivePortal.Helpers
{
    public interface IDotEnvConfiguration { }
    
    public partial class DotEnvConfiguration<TSelf>
        : IDotEnvConfiguration
        where TSelf : IDotEnvConfiguration, new()
    {
        [GeneratedRegex(@"^[A-Z0-9_]+$")]
        private static partial Regex EnvKeyRegex();

        [GeneratedRegex(@"^[']([^']*[^'\\]?)[']|^[\""]([^\""]*[^\""\\]?)[\""]|^[`]([^`]*[^`\\]?)[`]+")]
        private static partial Regex EnvQuotedValueRegex();

        [GeneratedRegex(@"^['\""`]")]
        private static partial Regex EnvValueFirstCharQuote();

        /// <summary>
        /// Loads the env file
        /// </summary>
        /// <param name="path">The path to the env file</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">Throws when the file at path cannot be found</exception>
        /// <exception cref="IOException">Throws when the file cannot be opened for reading</exception>
        /// <exception cref="FormatException">Throws when there is a formatting issue with the dotenv file</exception>
        public static async Task<Dictionary<string, string>> LoadDotEnvAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(path)) throw new FileNotFoundException();
            using StreamReader sr = new StreamReader(path);
            int lineNumber = 0;

            Dictionary<string, string> results = new();

            while (!sr.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                string? line = await sr.ReadLineAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested) break;

                lineNumber++;

                if (line is null) continue;

                line = line.Trim();
                if (line.Length < 1) continue;
                if (line[0] == '#') continue;

                int seperatorPosition = line.IndexOf('=');
                if (seperatorPosition < 1)
                    throw new FormatException($"Non comment line has no seperator \'=\' symbol on line {lineNumber}");

                string key = line.Substring(0, seperatorPosition).Trim().ToUpper();
                if (!EnvKeyRegex().Match(key).Success)
                    throw new FormatException($"Key uses invalid characters on line {lineNumber}");
                if (results.ContainsKey(key))
                    throw new FormatException($"Duplicate Key on line {lineNumber}");

                string rawValue = line.Substring(seperatorPosition + 1).Trim();

                string value = string.Empty;
                Match quotedValueMatch = EnvQuotedValueRegex().Match(rawValue);
                if (quotedValueMatch.Success && quotedValueMatch.Groups.Count > 0)
                {
                    // Work backwards through the groups to find the one that matched
                    // Plug that regex into regex101 for help
                    // Group 0 = with quotes
                    // Group 1/2/3 = without quotes depending on which quote was used '/"/`
                    for (int i = quotedValueMatch.Groups.Count - 1; i >= 0; i--)
                    {
                        value = quotedValueMatch.Groups[i].Value;
                        if (!string.IsNullOrWhiteSpace(value)) break;
                    }

                    if (rawValue.Length > (value.Length + 2))
                    {
                        string afterQuotes = rawValue.Substring(value.Length + 2).Trim();

                        if (afterQuotes.Length > 0 &&
                            afterQuotes[0] != '#')
                        {
                            throw new FormatException($"Found non comment data after quoted value on line {lineNumber}");
                        }
                    }
                }
                else if (EnvValueFirstCharQuote().Match(rawValue).Success)
                {
                    throw new FormatException($"Missing end quotes on line {lineNumber}");
                }
                else
                {
                    int commentPosition = rawValue.IndexOf('#');
                    if (commentPosition >= 0)
                    {
                        rawValue = rawValue.Substring(0, commentPosition).Trim();
                    }

                    value = rawValue;
                }

                results.Add(key, value);
                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
            }

            return results;
        }

        protected enum EnvTypes
        {
            STRING,
            PARSEABLE,
            LIST
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
        protected class EnvAttribute : Attribute
        {
            public string Name { get; private set; }
            public bool Optional { get; private set; }
            public string? Default { get; private set; }
            public EnvTypes Type { get; private set; }

            protected EnvAttribute(string name, bool optional, string? defaultValue, EnvTypes type)
            {
                this.Name = name;
                this.Optional = optional;
                this.Default = defaultValue;
                this.Type = type;
            }

            public EnvAttribute(string name, bool optional = false, string? defaultValue = null)
                : this(name, optional, defaultValue, EnvTypes.STRING) { }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
        protected class EnvAttribute<T>(
            string name,
            bool optional = false,
            string? defaultValue = null)
            : EnvAttribute(name, optional, defaultValue, EnvTypes.PARSEABLE)
            where T : IParsable<T>
        { }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
        protected class EnvListAttribute(
            string name,
            bool optional = false,
            string? defaultValue = null)
            : EnvAttribute(name, optional, defaultValue, EnvTypes.LIST)
        { }

        /// <summary>
        /// Loads the env file, sets the environment variables, and then builds and returns the configuration
        /// </summary>
        /// <param name="path">The path to the env file</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Throws when there is a reflection issue. Should only occur if this class is broken by a code change</exception>
        /// <exception cref="AggregateException">Throws when an environment variable is missing or invalid. See inner exceptions for details</exception>
        /// <exception cref="FileNotFoundException">Throws when the file at path cannot be found</exception>
        /// <exception cref="IOException">Throws when the file cannot be opened for reading</exception>
        public static async Task<TSelf> FromEnvFileAsync(string path, CancellationToken cancellationToken = default)
        {
            await LoadDotEnvAsync(path, cancellationToken);
            return new TSelf();
        }

        private static void SetValue(PropertyInfo property, object instance)
        {
            EnvAttribute? env = property.GetCustomAttribute<EnvAttribute>();
            if (env is null) throw new InvalidOperationException();

            string? rawEnv = Environment.GetEnvironmentVariable(env.Name);

            if (rawEnv is null)
            {
                if (!env.Optional && env.Default is null)
                {
                    throw new MissingFieldException(env.Name);
                }
                else if (env.Default is not null)
                {
                    rawEnv = env.Default;
                }
            }

            object? value = null;

            switch (env.Type)
            {
                case EnvTypes.STRING:
                    value = rawEnv;
                    break;
                case EnvTypes.PARSEABLE:
                    Type? parseType = env.GetType().GetGenericArguments()
                        .Where(x => x.BaseType != typeof(DotEnvConfiguration<TSelf>))
                        .FirstOrDefault();
                    if (parseType is null) throw new InvalidOperationException();

                    MethodInfo? tryParse = parseType
                        .GetMethod("TryParse", [typeof(string), parseType.MakeByRefType()]);
                    if (tryParse is null) throw new InvalidOperationException();

                    object?[] parameters = [rawEnv, null];
                    object? result = tryParse.Invoke(null, parameters);
                    if (result is null || !(bool)result)
                    {
                        throw new InvalidCastException(env.Name);
                    }

                    value = parameters[1];

                    break;
                case EnvTypes.LIST:
                    value = rawEnv?.Split(',')
                        .Select(x => x.Trim())
                        .ToList() ?? [];
                    break;
            }

            property.SetValue(instance, value);
        }

        public DotEnvConfiguration()
        {
            List<PropertyInfo> properties = typeof(TSelf)
                .GetProperties()
                .Where(x => x.GetCustomAttribute<EnvAttribute>() != null)
                .ToList();

            List<string> missing = new();
            List<string> invalid = new();

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    SetValue(property, this);
                }
                catch (MissingFieldException ex)
                {
                    missing.Add(ex.Message);
                }
                catch (InvalidCastException ex)
                {
                    missing.Add(ex.Message);
                }
            }

            if (missing.Any() || invalid.Any())
            {
                List<Exception> exs = new();

                if (missing.Any())
                {
                    StringBuilder sb = new();
                    sb.Append("The following required Environment Variables are missing:");
                    foreach (string m in missing)
                    {
                        sb.Append($" '{m}'");
                    }
                    exs.Add(new MissingFieldException(sb.ToString()));
                }

                if (invalid.Any())
                {
                    StringBuilder sb = new();
                    sb.Append("The following required Environment Variables are invalid:");
                    foreach (string i in invalid)
                    {
                        sb.Append($" '{i}'");
                    }
                    exs.Add(new InvalidCastException(sb.ToString()));
                }

                throw new AggregateException(
                    "One or more Environment Variables is missing or invalid. See inner exceptions!",
                    exs);
            }
        }
    }
}
