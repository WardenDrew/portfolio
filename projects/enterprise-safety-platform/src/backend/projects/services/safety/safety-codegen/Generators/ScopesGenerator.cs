using Platform.Legacy.CodeGen.Languages;
using Platform.Legacy.CodeGen.Models;

namespace Platform.Legacy.CodeGen.Generators;

public class ScopesGenerator : IGenerator
{
	private const string OUTPUT_PATH = "src/Platform.Legacy.Common/Constants/Scopes/";
	private const string OUTPUT_NAMESPACE = "Platform.Legacy.Common.Constants.Scopes";

	private static readonly PTree openId = PTree
		.CreateBuilder(string.Empty)
		.Description("OpenID Scopes used for Identity Tokens")
		.AddEdge(
			value: "openid",
			description: "The mandatory openid scope to elevate from OAuth2 to OIDC. Requires the \'sub\' claim to be present."
		)
		.AddEdge(
			value: "profile",
			description: "Scope to receive at a minimum \'name\' but possibly including additional user profile details"
		)
		.AddEdge(value: "email", description: "Scope to receive the \'email\' and \'email_verified\' claims")
		.AddEdge(value: "phone", description: "Scope to receive the \'phone_number\' and \'phone_number_verified\' claims")
		.AddEdge(value: "offline_access", description: "Scope to request a refresh token is issued alongside an acces token")
		.AsTree();

	private static readonly PTree modules = PTree
		.CreateBuilder("modules")
		.Description("Scopes to indicate with frontend modules should be visible")
		.AddPermission(value: "all", description: "Access to all modules")
		.MakeGroup(out string allModules)
		.AddPermission(value: "ga", description: "General-Availability modules that are active for everyone by default")
		.MakeGroup(out string gaModules)
		.AddPermission(value: "beta", description: "Modules that are in Public-Beta")
		.MakeGroup(out string betaModules)
		.AddEdge(value: "core", description: "Core modules that are needed for basic functionality, such as authentication and account")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "assets", description: "Access to Assets")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "timeclock", description: "Access to the Timeclock")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "issues", description: "Access to Log-It Issues")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "forms", description: "Access to Forms")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "messaging", description: "Access to Messaging")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "documents", description: "Access to Documents")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "training", description: "Access to Training")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "billing", description: "Access to Billing")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "reports", description: "Access to Reports")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "store", description: "Access to the Store")
		.Group(allModules)
		.Group(gaModules)
		.AddEdge(value: "planners", description: "Access to Planners")
		.Group(allModules)
		.Group(betaModules)
		.AsTree();

	public Task<List<CodeGeneratorResult>> Generate(string[] fileComment)
	{
		List<CodeGeneratorResult> result =
		[
			new()
			{
				Code =
					CSharp.BlockComment(fileComment)
					+ $"{CSharp.NEWLINE}{CSharp.NEWLINE}{CSharp.Namespace(ScopesGenerator.OUTPUT_NAMESPACE)}{CSharp.NEWLINE}{CSharp.NEWLINE}"
					+ ScopesGenerator.openId.ToCSharpClass(className: "OpenId", attributes: [], modifiers: [CSharp.PUBLIC,]),
				Language = SupportedLanguages.CSharp,
				OutputPath = Path.Combine(path1: ScopesGenerator.OUTPUT_PATH, path2: "OpenId.cs"),
			},
			new()
			{
				Code =
					CSharp.BlockComment(fileComment)
					+ $"{CSharp.NEWLINE}{CSharp.NEWLINE}{CSharp.Namespace(ScopesGenerator.OUTPUT_NAMESPACE)}{CSharp.NEWLINE}{CSharp.NEWLINE}"
					+ ScopesGenerator.modules.ToCSharpClass(className: "Modules", attributes: [], modifiers: [CSharp.PUBLIC,]),
				Language = SupportedLanguages.CSharp,
				OutputPath = Path.Combine(path1: ScopesGenerator.OUTPUT_PATH, path2: "Modules.cs"),
			},
		];

		return Task.FromResult(result);
	}
}
