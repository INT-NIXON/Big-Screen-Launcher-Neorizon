using System;
using Avalonia;
using PCL.Core.App.IoC;
using Windows.Gaming.Input;

namespace Big_Screen_Launcher_Neorizon;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Lifecycle.OnInitialize();

        // Force WGI to enumerate gamepads (initializes WinRT factory early)
        try { var _ = Gamepad.Gamepads.Count; } catch { }

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
