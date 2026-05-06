using System;
using Avalonia;
using PCL.Core.App.IoC;

namespace Big_Screen_Launcher_Neorizon;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Lifecycle.OnInitialize();

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Lifecycle.Shutdown();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont();
}
