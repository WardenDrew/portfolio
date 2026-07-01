namespace Platform.Legacy.CodeGen.Languages;

public partial class CSharp
{
	public static readonly string NOTHING = string.Empty;
	public const string TAB = "\t";
	public const string NEWLINE = "\n";

	public const string LONG_SUFFIX = "L";
	public const string FLOAT_SUFFIX = "F";
	public const string DOUBLE_SUFFIX = "D";
	public const string DECIMAL_SUFFIX = "M";
	public const string UNSIGNED_SUFFIX = "U";
	public const string HEX_PREFIX = "0x";

	public static string Literal(string value)
	{
		return $"\"{value}\"";
	}

	public static string Literal(char value)
	{
		return $"\'{value}\'";
	}

	public static string Literal(bool value)
	{
		return $"{value.ToString().ToLower()}";
	}

	public static string Literal(short value)
	{
		return $"{value}";
	}

	public static string Literal(int value)
	{
		return $"{value}";
	}

	public static string Literal(long value)
	{
		return $"{value}{CSharp.LONG_SUFFIX}";
	}

	public static string Literal(float value)
	{
		return $"{value}{CSharp.FLOAT_SUFFIX}";
	}

	public static string Literal(double value)
	{
		return $"{value}{CSharp.DOUBLE_SUFFIX}";
	}

	public static string Literal(decimal value)
	{
		return $"{value}{CSharp.DECIMAL_SUFFIX}";
	}

	public static string Literal(ushort value)
	{
		return $"{value}";
	}

	public static string Literal(uint value)
	{
		return $"{value}{CSharp.UNSIGNED_SUFFIX}";
	}

	public static string Literal(ulong value)
	{
		return $"{value}{CSharp.UNSIGNED_SUFFIX}{CSharp.LONG_SUFFIX}";
	}

	public static string Literal(byte value)
	{
		return $"{CSharp.HEX_PREFIX}{value:X2}";
	}

	public static string Literal(sbyte value)
	{
		return $"{CSharp.HEX_PREFIX}{value:X2}";
	}

	public static string Assign(string lhs, string rhs)
	{
		return $"{lhs} = {rhs}";
	}

	public static string Increment(string lhs)
	{
		return $"{lhs}++";
	}

	public static string Decrement(string lhs)
	{
		return $"{lhs}--";
	}

	public static string Add(string lhs, string rhs)
	{
		return $"{lhs} + {rhs}";
	}

	public static string Subtract(string lhs, string rhs)
	{
		return $"{lhs} - {rhs}";
	}

	public static string Multiply(string lhs, string rhs)
	{
		return $"{lhs} * {rhs}";
	}

	public static string Divide(string lhs, string rhs)
	{
		return $"{lhs} / {rhs}";
	}

	public static string Modulo(string lhs, string rhs)
	{
		return $"{lhs} % {rhs}";
	}

	public static string Negate(string rhs)
	{
		return $"!{rhs}";
	}

	public static string BinaryAnd(string lhs, string rhs)
	{
		return $"{lhs} & {rhs}";
	}

	public static string BinaryOr(string lhs, string rhs)
	{
		return $"{lhs} | {rhs}";
	}

	public static string BinaryXor(string lhs, string rhs)
	{
		return $"{lhs} ^ {rhs}";
	}

	public static string And(string lhs, string rhs)
	{
		return $"{lhs} && {rhs}";
	}

	public static string Or(string lhs, string rhs)
	{
		return $"{lhs} || {rhs}";
	}

	public static string Complement(string rhs)
	{
		return $"~{rhs}";
	}

	public static string LeftShift(string lhs, string rhs)
	{
		return $"{lhs} << {rhs}";
	}

	public static string RightShift(string lhs, string rhs)
	{
		return $"{lhs} >> {rhs}";
	}

	public static string UnsignedRightShift(string lhs, string rhs)
	{
		return $"{lhs} >>> {rhs}";
	}

	public static string[] SplitLines(string input)
	{
		return input.Split(CSharp.NEWLINE);
	}

	public static string[] SplitLines(string[] input)
	{
		return [.. input.SelectMany(item => item.Split(CSharp.NEWLINE)),];
	}

	public static string[] PrependAppend(
		string[] input,
		string? prepend,
		string? append,
		bool skipFirstPrepend,
		bool skipLastAppend
	)
	{
		return [.. input
			.Select((x, i) =>
				$"{(skipFirstPrepend && i == 0 ? CSharp.NOTHING : prepend)}"
				+ x
				+ $"{(skipLastAppend && i == input.Length - 1 ? CSharp.NOTHING : append)}"
			),];
	}

	public static string[] Prepend(string[] input, string? prepend, bool skipFirst)
	{
		return CSharp.PrependAppend(input: input, prepend: prepend, append: null, skipFirstPrepend: skipFirst,
			skipLastAppend: false);
	}

	public static string[] Append(string[] input, string? append, bool skipLast)
	{
		return CSharp.PrependAppend(input: input, prepend: null, append: append, skipFirstPrepend: false,
			skipLastAppend: skipLast);
	}

	public static string[] Indent(string[] input)
	{
		return CSharp.Prepend(input: CSharp.SplitLines(input), prepend: CSharp.TAB, skipFirst: false);
	}

	public static string Joiner(
		string[] items,
		bool splitLines,
		string? prepend,
		string? append,
		string? itemsPrepend,
		string? itemsAppend,
		bool skipFirstPrepend,
		bool skipLastAppend
	)
	{
		return (prepend is not null ? $"{prepend}{(splitLines ? CSharp.NEWLINE : CSharp.NOTHING)}" : CSharp.NOTHING)
			+ (
				splitLines
					? string.Join(
						separator: $"{CSharp.NEWLINE}",
						value: CSharp.Indent(CSharp.PrependAppend(input: items, prepend: itemsPrepend, append: itemsAppend,
							skipFirstPrepend: skipFirstPrepend, skipLastAppend: skipLastAppend))
					)
					: string.Join(separator: " ",
						value: CSharp.PrependAppend(input: items, prepend: itemsPrepend, append: itemsAppend,
							skipFirstPrepend: skipFirstPrepend, skipLastAppend: skipLastAppend))
			)
			+ (append is not null ? $"{(splitLines ? CSharp.NEWLINE : CSharp.NOTHING)}{append}" : CSharp.NOTHING);
	}

	public static string Collection(string[] items, bool splitLines)
	{
		return CSharp.Joiner(items: items, splitLines: splitLines, prepend: "[", append: "]", itemsPrepend: null,
			itemsAppend: ",", skipFirstPrepend: false, skipLastAppend: false);
	}

	public static string Parameters(string[] items, bool splitLines)
	{
		return CSharp.Joiner(items: items, splitLines: splitLines, prepend: "(", append: ")", itemsPrepend: null,
			itemsAppend: ",", skipFirstPrepend: false, skipLastAppend: true);
	}

	public static string TypeArguments(string[] items, bool splitLines)
	{
		return CSharp.Joiner(items: items, splitLines: splitLines, prepend: "<", append: ">", itemsPrepend: null,
			itemsAppend: ",", skipFirstPrepend: false, skipLastAppend: true);
	}

	public static string BlockCollection(string[] items, bool trailingComma)
	{
		return CSharp.Joiner(items: items, splitLines: true, prepend: "{", append: "}", itemsPrepend: null, itemsAppend: ",",
			skipFirstPrepend: false, skipLastAppend: !trailingComma);
	}

	public static string Block(string[] items)
	{
		return CSharp.Joiner(items: items, splitLines: true, prepend: "{", append: "}", itemsPrepend: null,
			itemsAppend: CSharp.NEWLINE, skipFirstPrepend: false, skipLastAppend: true);
	}

	public static string Spread(string rhs)
	{
		return $"..{rhs}";
	}

	public static string Equals(string lhs, string rhs)
	{
		return $"{lhs} == {rhs}";
	}

	public static string NotEquals(string lhs, string rhs)
	{
		return $"{lhs} != {rhs}";
	}

	public static string LessThan(string lhs, string rhs)
	{
		return $"{lhs} < {rhs}";
	}

	public static string GreaterThan(string lhs, string rhs)
	{
		return $"{lhs} > {rhs}";
	}

	public static string LessThanEquals(string lhs, string rhs)
	{
		return $"{lhs} <= {rhs}";
	}

	public static string GreaterThanEquals(string lhs, string rhs)
	{
		return $"{lhs} >={rhs}";
	}

	public static string Member(string rhs)
	{
		return $".{rhs}";
	}

	public static string Element(string element)
	{
		return $"[{element}]";
	}

	public static string NullConditional(string rhs)
	{
		return $"?{rhs}";
	}

	public static string Invoke(string[] typeArguments, string[] parameters, bool splitLines)
	{
		return CSharp.TypeArguments(items: typeArguments, splitLines: splitLines) +
			CSharp.Parameters(items: parameters, splitLines: splitLines);
	}

	public static string IndexFromEnd(string rhs)
	{
		return $"^{rhs}";
	}

	public static string Range(string? lhs, string? rhs)
	{
		return (lhs ?? CSharp.NOTHING) + ".." + (rhs ?? CSharp.NOTHING);
	}

	public static string Is(string lhs, string typeName, string? variableName)
	{
		return $"{lhs} {CSharp.IS} {typeName}" + (variableName is not null ? $" {variableName}" : CSharp.NOTHING);
	}

	public static string IsNot(string lhs, string typeName, string? variableName)
	{
		return $"{lhs} {CSharp.IS} {CSharp.NOT} {typeName}" + (variableName is not null ? $" {variableName}" : CSharp.NOTHING);
	}

	public static string As(string lhs, string typeName)
	{
		return $"{lhs} {CSharp.AS} {typeName}";
	}

	public static string Cast(string typeName, string rhs)
	{
		return $"({typeName}){rhs}";
	}

	public static string Cast(string value)
	{
		return $"{CSharp.TYPEOF}({value})";
	}

	public static string Default(string type)
	{
		return $"{CSharp.DEFAULT}({type})";
	}

	public static string Comment(string[] comments)
	{
		return "// " + string.Join(separator: $"{CSharp.NEWLINE}// ", value: comments);
	}

	public static string BlockComment(string[] comments)
	{
		return "/* " + string.Join(separator: $"{CSharp.NEWLINE} * ", value: comments) + $"{CSharp.NEWLINE}*/";
	}

	public static string XmlDocTag(string tag, string inner)
	{
		return $"/// <{tag}>{CSharp.NEWLINE}" + $"/// {inner}{CSharp.NEWLINE}" + $"/// </{tag}>";
	}

	public static string UsingDirective(string usingInclude)
	{
		return $"{CSharp.USING} {usingInclude};";
	}

	public static string UsingDirective(string usingInclude, bool isGlobal, bool isStatic, string? alias = null)
	{
		return (isGlobal ? $"{CSharp.GLOBAL}" : CSharp.NOTHING) + $"{CSharp.USING} {usingInclude};";
	}

	public static string Namespace(string name)
	{
		return $"{CSharp.NAMESPACE} {name};";
	}

	public static string NamespaceBlock(string name, string[] namespaceParts)
	{
		return $"{CSharp.NAMESPACE} {name}" + CSharp.Block(namespaceParts);
	}

	public static string Constraint(string type, string[] inherits)
	{
		return $"{CSharp.WHERE} {type} : " + string.Join(separator: ", }", value: inherits);
	}

	public static string Class(
		string name,
		string? xmlDoc,
		string[] attributes,
		string[] modifiers,
		string[] typeArguments,
		string[] inherits,
		string[] constraints,
		string[] classParts
	)
	{
		return (xmlDoc is not null ? $"{xmlDoc}{CSharp.NEWLINE}" : CSharp.NOTHING)
			+ (attributes.Length > 0 ? $"{string.Join(separator: CSharp.NEWLINE, value: attributes)}{CSharp.NEWLINE}" : CSharp.NOTHING)
			+ (modifiers.Length > 0 ? $"{string.Join(separator: " ", value: modifiers)} " : CSharp.NOTHING)
			+ $"{CSharp.CLASS} {name}"
			+ (typeArguments.Length > 0 ? CSharp.TypeArguments(items: typeArguments, splitLines: false) : CSharp.NOTHING)
			+ (inherits.Length > 0 ? $" : {string.Join(separator: ", ", value: inherits)}" : CSharp.NOTHING)
			+ (constraints.Length > 0
				? $"{CSharp.NEWLINE}{CSharp.TAB}{string.Join(separator: $"{CSharp.NEWLINE}{CSharp.TAB}", value: constraints)}"
				: CSharp.NOTHING)
			+ CSharp.NEWLINE
			+ CSharp.Block(classParts);
	}

	public static string Field(
		string name,
		string? xmlDoc,
		string[] attributes,
		string[] modifiers,
		string type,
		string? rhs
	)
	{
		return (xmlDoc is not null ? $"{xmlDoc}{CSharp.NEWLINE}" : CSharp.NOTHING)
			+ (attributes.Length > 0 ? $"{string.Join(separator: CSharp.NEWLINE, value: attributes)}{CSharp.NEWLINE}" : CSharp.NOTHING)
			+ (modifiers.Length > 0 ? $"{string.Join(separator: " ", value: modifiers)} " : CSharp.NOTHING)
			+ $"{type} {name}"
			+ (rhs is not null ? $" = {rhs}" : CSharp.NOTHING)
			+ ";";
	}

	public static string Property(
		string name,
		string? xmlDoc,
		string[] attributes,
		string[] modifiers,
		string type,
		string propertyDeclaration
	)
	{
		return (xmlDoc is not null ? $"{xmlDoc}{CSharp.NEWLINE}" : CSharp.NOTHING)
			+ (attributes.Length > 0 ? $"{string.Join(separator: CSharp.NEWLINE, value: attributes)}{CSharp.NEWLINE}" : CSharp.NOTHING)
			+ (modifiers.Length > 0 ? $"{string.Join(separator: " ", value: modifiers)} " : CSharp.NOTHING)
			+ $"{type} {name} {propertyDeclaration}";
	}

	public enum AutoPropertyType
	{
		GET,
		SET,
		INIT,
		GET_SET,
		GET_INIT,
	}

	public static string AutoPropertyDeclaration(
		AutoPropertyType autoPropertyType,
		string[] getterModifiers,
		string[] setterModifiers,
		string? initialValue
	)
	{
		return autoPropertyType switch
		{
			AutoPropertyType.GET => "{ "
				+ $"{(getterModifiers.Length > 0 ? $"{string.Join(separator: " ", value: getterModifiers)} " : CSharp.NOTHING)}"
				+ $"{CSharp.GET}; }}",
			AutoPropertyType.SET => "{ "
				+ $"{(setterModifiers.Length > 0 ? $"{string.Join(separator: " ", value: setterModifiers)} " : CSharp.NOTHING)}"
				+ $"{CSharp.SET}; }}",
			AutoPropertyType.INIT => $"{{ {CSharp.INIT}; }}",
			AutoPropertyType.GET_SET => "{ "
				+ $"{(getterModifiers.Length > 0 ? $"{string.Join(separator: " ", value: getterModifiers)} " : CSharp.NOTHING)}"
				+ $"{CSharp.GET}; "
				+ $"{(setterModifiers.Length > 0 ? $"{string.Join(separator: " ", value: setterModifiers)} " : CSharp.NOTHING)}"
				+ $"{CSharp.SET}; }}",
			AutoPropertyType.GET_INIT => "{ "
				+ $"{(getterModifiers.Length > 0 ? $"{string.Join(separator: " ", value: getterModifiers)} " : CSharp.NOTHING)}"
				+ $"{CSharp.GET}; {CSharp.INIT}; }}",
			_ => throw new ArgumentOutOfRangeException(nameof(autoPropertyType)),
		} + (initialValue is not null ? $" = {initialValue};" : CSharp.NOTHING);
	}
}
