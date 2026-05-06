using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Big_Screen_Launcher_Neorizon.Services;
using Big_Screen_Launcher_Neorizon.ViewModels;
using BSLN.Core.Application;
using BSLN.Core.Application.Abstractions;
using BSLN.Core.Catalog;
using BSLN.Core.Input;
using BSLN.Core.Launch;
using BSLN.Core.Settings;
using BSLN.Core.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace Big_Screen_Launcher_Neorizon;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BigScreenLauncherNeorizon");

        var catalogPath = Path.Combine(appDataDirectory, "catalog.json");
        var settingsPath = Path.Combine(appDataDirectory, "settings.json");

        var services = new ServiceCollection();
        services.AddSingleton<IGameCatalogRepository>(_ => new JsonGameCatalogRepository(catalogPath));
        services.AddSingleton<ISettingsRepository>(_ => new JsonSettingsRepository(settingsPath));
        services.AddSingleton<IGameLauncher, WindowsProcessLauncher>();
        services.AddSingleton<IInputGlyphCatalog, ResourceGlyphCatalog>();
        services.AddSingleton<ControllerActionTranslator>();
        services.AddSingleton<IControllerInputSource, XInputPollingService>();
        services.AddSingleton<IDiscoveredGameSource, SteamDiscoveryService>();
        services.AddSingleton<IDiscoveredGameSource, XboxDiscoveryService>();
        services.AddSingleton<IGameDiscoveryAggregator, AggregateDiscoveryService>();
        services.AddSingleton<ShellStateReducer>();
        services.AddSingleton<InputHintService>();
        services.AddSingleton<LibraryShellService>();
        services.AddSingleton<LibraryShellViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<WindowPresentationService>();
        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }
}
