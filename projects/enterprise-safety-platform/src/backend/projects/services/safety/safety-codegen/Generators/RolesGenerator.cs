using Platform.Legacy.CodeGen.Languages;
using Platform.Legacy.CodeGen.Models;

namespace Platform.Legacy.CodeGen.Generators;

public class RolesGenerator : IGenerator
{
	private const string OUTPUT_PATH = "src/Platform.Legacy.Common/Constants/Scopes/Roles.cs";
	private const string OUTPUT_NAMESPACE = "Platform.Legacy.Common.Constants.Scopes";
	private const string OUTPUT_CLASS = "Roles";

	private static readonly PTree roles = PTree
		.CreateBuilder("roles")
		.Description("Roles to define broad permissions boundaries")
		.AddNode("org")
		.AddEdge(value: "employee", description: "Basic role for organization employees")
		.AddEdge(value: "contractor", description: "Basic role for organization contractors")
		.AddEdge(value: "manager", description: "User within an organization that manages other users")
		.AddEdge(value: "human_resources", description: "User with access to HR PII systems")
		.AddEdge(value: "billing", description: "User with access to billing / invoicing details")
		.AddEdge(value: "payroll", description: "User with access to payroll processing systems")
		.AddEdge(value: "super_admin", description: "User with full control of a company")
		.Ascend()
		.AddNode("sys")
		.AddEdge(value: "sales", description: "Sales Team")
		.AddEdge(value: "support", description: "Support Team")
		.AddEdge(value: "accounting", description: "Accounting team")
		.AddEdge(value: "developer", description: "Development Team")
		.Ascend()
		.AsTree();

	public Task<List<CodeGeneratorResult>> Generate(string[] fileComment)
	{
		List<CodeGeneratorResult> result = [];

		string cSharp = CSharp.BlockComment(fileComment);

		cSharp +=
			$"{CSharp.NEWLINE}{CSharp.NEWLINE}{CSharp.Namespace(RolesGenerator.OUTPUT_NAMESPACE)}{CSharp.NEWLINE}{CSharp.NEWLINE}";
		cSharp += RolesGenerator.roles.ToCSharpClass(className: RolesGenerator.OUTPUT_CLASS, attributes: [], modifiers: [CSharp.PUBLIC,]);

		result.Add(
			new CodeGeneratorResult
			{
				Code = cSharp,
				Language = SupportedLanguages.CSharp,
				OutputPath = RolesGenerator.OUTPUT_PATH,
			}
		);

		return Task.FromResult(result);
	}
}
