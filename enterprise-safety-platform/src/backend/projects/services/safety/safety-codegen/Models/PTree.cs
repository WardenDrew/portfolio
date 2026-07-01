using System.Text.RegularExpressions;
using Platform.Legacy.CodeGen.Languages;

namespace Platform.Legacy.CodeGen.Models;

public partial class PTree : PTreeNode
{
	public const char DEFAULT_NODE_SEPARATOR = '.';
	public const char DEFAULT_PERMISSION_SEPARATOR = ':';

	public char NodeSeparator { get; set; }
	public char PermissionSeparator { get; set; }

	public Dictionary<string, List<string>> KnownGroups { get; set; } = new();

	public Dictionary<string, string> FlattenGroups()
	{
		return KnownGroups.ToDictionary(keySelector: kvp => kvp.Key,
			elementSelector: kvp => string.Join(separator: " ", values: kvp.Value));
	}

	public Dictionary<string, string?> FlattenedTree { get; set; } = new();

	public static PTreeNodeBuilder CreateBuilder(
		string value,
		string? prefix = null,
		char? nodeSeparator = null,
		char? permissionSeparator = null
	)
	{
		return new PTreeNodeBuilder(
			value: value,
			prefix: prefix ?? string.Empty,
			nodeSeparator: nodeSeparator ?? PTree.DEFAULT_NODE_SEPARATOR,
			permissionSeparator: permissionSeparator ?? PTree.DEFAULT_PERMISSION_SEPARATOR
		);
	}

	[GeneratedRegex(@"[^\w]+")]
	private static partial Regex VariableNameRegex();

	public string ToCSharpClass(string className, string[] attributes, string[] modifiers)
	{
		List<string> classParts = [];
		List<string> valuesDictionaryEntries = [];
		List<string> groupsDictionaryEntries = [];

		foreach (KeyValuePair<string, string?> kvp in this.FlattenedTree)
		{
			string variableName = PTree.VariableNameRegex().Replace(input: kvp.Key, replacement: "_").ToUpperInvariant();
			string description = kvp.Value ?? "NO DESCRIPTION";

			// Add the const field
			classParts.Add(
				CSharp.Field(
					name: variableName,
					xmlDoc: CSharp.XmlDocTag(tag: "summary", inner: description),
					attributes: [],
					modifiers: [CSharp.PUBLIC, CSharp.CONST,],
					type: "string",
					rhs: CSharp.Literal(kvp.Key)
				)
			);

			// Add to values dictionary
			valuesDictionaryEntries.Add(
				CSharp.BlockCollection(items: [CSharp.Literal(kvp.Key), CSharp.Literal(description),], trailingComma: false)
			);
		}

		foreach (KeyValuePair<string, List<string>> kvp in this.KnownGroups)
		{
			groupsDictionaryEntries.Add(
				CSharp.BlockCollection(
					items: [CSharp.Literal(kvp.Key), CSharp.Collection(items: [.. kvp.Value.Select(CSharp.Literal),], splitLines: true),],
					trailingComma: false
				)
			);
		}

		classParts.Add(
			CSharp.Field(
				name: "Values",
				xmlDoc: CSharp.XmlDocTag(tag: "summary", inner: "All Possible Values"),
				attributes: [],
				modifiers: [CSharp.PUBLIC, CSharp.STATIC, CSharp.READONLY,],
				type: "Dictionary<string,string>",
				rhs: $"{CSharp.NEW}()"
					+ (
						valuesDictionaryEntries.Count > 0
							? $"{CSharp.NEWLINE}{CSharp.BlockCollection(items: [.. valuesDictionaryEntries,], trailingComma: true)}"
							: CSharp.NOTHING
					)
			)
		);

		classParts.Add(
			CSharp.Field(
				name: "Groups",
				xmlDoc: CSharp.XmlDocTag(tag: "summary", inner: "Value Groupings"),
				attributes: [],
				modifiers: [CSharp.PUBLIC, CSharp.STATIC, CSharp.READONLY,],
				type: "Dictionary<string,List<string>>",
				rhs: $"{CSharp.NEW}()"
					+ (
						groupsDictionaryEntries.Count > 0
							? $"{CSharp.NEWLINE}{CSharp.BlockCollection(items: [.. groupsDictionaryEntries,], trailingComma: true)}"
							: CSharp.NOTHING
					)
			)
		);

		return CSharp.Class(
			name: className,
			xmlDoc: CSharp.XmlDocTag(tag: "summary", inner: this.Description ?? "NO DESCRIPTION"),
			attributes: attributes,
			modifiers: modifiers,
			typeArguments: [],
			inherits: [],
			constraints: [],
			classParts: [.. classParts,]
		);
	}
}
