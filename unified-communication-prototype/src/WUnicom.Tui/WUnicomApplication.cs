using Spectre.Console;
using WUnicom.Provider.Dummy;
using WUnicom.Tui.Models;
using WUnicom.Tui.Pages;
using WUnicom.Tui.Rendering;
using WUnicom.Tui.State;

namespace WUnicom.Tui;

public sealed class WUnicomApplication
{
    private readonly IReadOnlyList<ConfiguredProvider> _configuredProviders =
    [
        new("Dummy", "Local in-memory provider for UI testing", new DummyUnifiedCommunicationProvider())
    ];
    private readonly ProviderTypeCatalog _providerCatalog;

    private readonly AppState _state = new();

    public WUnicomApplication()
    {
        _providerCatalog = new ProviderTypeCatalog(_configuredProviders);
    }

    public async Task RunAsync()
    {
        await AnsiConsole.Live(new Markup(string.Empty))
            .AutoClear(true)
            .StartAsync(async liveContext =>
            {
                var renderer = new ScreenRenderer(liveContext);

                while (!_state.ShouldExit)
                {
                    switch (_state.CurrentPage)
                    {
                        case AppPage.Home:
                            await HomePage.RunAsync(_providerCatalog, _state, renderer);
                            break;
                        case AppPage.AddConnection:
                            await AddConnectionPage.RunAsync(_state, renderer);
                            break;
                        case AppPage.LoginHandler:
                            await LoginHandlerPage.RunAsync(_state, renderer);
                            break;
                        case AppPage.Room:
                            await RoomPage.RunAsync(_state, renderer);
                            break;
                        case AppPage.RoomSelect:
                            await RoomSelectPage.RunAsync(_state, renderer);
                            break;
                        case AppPage.Help:
                            await HelpPage.RunAsync(_state, renderer);
                            break;
                        case AppPage.History:
                            await HistoryPage.RunAsync(_state, renderer);
                            break;
                    }
                }
            });

        AnsiConsole.Clear();
    }
}
