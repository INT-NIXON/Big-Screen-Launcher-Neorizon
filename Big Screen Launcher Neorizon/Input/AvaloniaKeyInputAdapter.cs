using Avalonia.Input;
using BSLN.Core.Domain;

namespace Big_Screen_Launcher_Neorizon.Input;

public static class AvaloniaKeyInputAdapter
{
    public static bool TryMap(Key key, out SemanticInputAction action)
    {
        action = key switch
        {
            Key.Left => SemanticInputAction.MoveLeft,
            Key.Right => SemanticInputAction.MoveRight,
            Key.Up => SemanticInputAction.MoveUp,
            Key.Down => SemanticInputAction.MoveDown,
            Key.Enter => SemanticInputAction.Accept,
            Key.Space => SemanticInputAction.Accept,
            Key.Escape => SemanticInputAction.Back,
            Key.Back => SemanticInputAction.Back,
            Key.PageUp => SemanticInputAction.PageLeft,
            Key.PageDown => SemanticInputAction.PageRight,
            Key.Apps => SemanticInputAction.Menu,
            _ => default,
        };

        return key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Enter or Key.Space or Key.Escape or Key.Back or Key.PageUp or Key.PageDown or Key.Apps;
    }
}
