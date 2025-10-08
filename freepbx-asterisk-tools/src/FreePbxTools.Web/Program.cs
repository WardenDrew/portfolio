using System.Security.Claims;
using FreePbxTools.Web.Components;
using FreePbxTools.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();

builder.Services
	.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/login";
		options.LogoutPath = "/logout";
		options.Cookie.HttpOnly = true;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
		options.Cookie.SameSite = SameSiteMode.Strict;
		options.SlidingExpiration = true;
		options.ExpireTimeSpan = TimeSpan.FromHours(1);
	});

builder.Services.AddAuthorizationBuilder()
	.SetDefaultPolicy(new AuthorizationPolicyBuilder()
		.RequireAuthenticatedUser()
		.Build());

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
	.AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<PagingService>();

builder.Services.AddHostedService<PageBackgroundWorker>();

WebApplication app = builder.Build();

using (IServiceScope startupScope = app.Services.CreateScope())
{
	SettingsService settingsService = startupScope.ServiceProvider.GetRequiredService<SettingsService>();
	await settingsService.Load();
	await settingsService.Save();
}

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseRouting();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost(
	"/api/login",
	async (
		HttpContext http, 
		SettingsService settings,
		[FromForm] string? password,
		[FromForm] string? returnUrl) =>
	{
		// Read password from posted form
		if (password != settings.Running.Password)
		{
			await Task.Delay(Random.Shared.Next(20, 80)); // small jitter
			return Results.Redirect("/login?failed");
		}

		ClaimsPrincipal principal 
			= new(new ClaimsIdentity(
				[
					new(ClaimTypes.Name, "embedded-user"),
				], 
				CookieAuthenticationDefaults.AuthenticationScheme));

		await http.SignInAsync(
			CookieAuthenticationDefaults.AuthenticationScheme, 
			principal);

		if (!string.IsNullOrWhiteSpace(returnUrl))
		{
			return Results.Redirect(returnUrl);
		}
		
		return Results.Redirect("/");
	}
);

app.MapMethods("/api/logout", ["GET", "POST",], async (HttpContext http) =>
{
	await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	return Results.Redirect("/login");
});

app.Run();