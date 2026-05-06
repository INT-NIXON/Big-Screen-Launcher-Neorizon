using Avalonia.Controls;

namespace Big_Screen_Launcher_Neorizon.Services;

public sealed class WindowPresentationService
{
    public void ApplyBigScreenPresentation(Window window, bool fullscreen)
    {
        window.WindowState = fullscreen ? WindowState.FullScreen : WindowState.Maximized;
        window.WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        window.CanResize = false;
    }
}
