namespace Platform.Legacy.CodeGen.Models;

public class CodeGeneratorResult
{
	public required string Code { get; set; }
	public required SupportedLanguages Language { get; set; }
	public required string OutputPath { get; set; }
}
