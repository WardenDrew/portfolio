using Platform.Legacy.CodeGen.Models;

namespace Platform.Legacy.CodeGen.Generators;

public interface IGenerator
{
	Task<List<CodeGeneratorResult>> Generate(string[] fileComment);
}
