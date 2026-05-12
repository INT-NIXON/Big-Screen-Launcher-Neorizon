using Avalonia.Input;

namespace Big_Screen_Launcher_Neorizon.Input;

/// <summary>
/// Maps Avalonia Key events to ControllerAction.
/// Ported from BigScreenLauncher (Rust) keyboard handling.
/// </summary>
public static class KeyActionMap
{
    public static ControllerAction? Map(Key key) => key switch
    {
        Key.Left => ControllerAction.MoveLeft,
        Key.Right => ControllerAction.MoveRight,
        Key.Up => ControllerAction.MoveUp,
        Key.Down => ControllerAction.MoveDown,
        Key.Enter => ControllerAction.Accept,
        Key.Space => ControllerAction.Accept,
        Key.Escape => ControllerAction.Back,
        Key.Back => ControllerAction.Back,
        Key.PageUp => ControllerAction.PageUp,
        Key.PageDown => ControllerAction.PageDown,
        _ => null,
    };
}
