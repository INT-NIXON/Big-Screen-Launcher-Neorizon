namespace Big_Screen_Launcher_Neorizon.Input;

/// <summary>
/// Abstracted UI actions, ported from BigScreenLauncher (Rust) ControllerAction enum.
/// Consumer (e.g. MainWindow) maps these to app behavior.
/// </summary>
public enum ControllerAction
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    PageUp,
    PageDown,
    Accept,
    Back,
    Menu,
    Refresh,
    Settings,
}
