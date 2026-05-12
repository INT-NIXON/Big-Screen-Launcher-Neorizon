namespace Big_Screen_Launcher_Neorizon.Input;

/// <summary>
/// Identifies the active controller type. Drives glyph icon selection (Xbox vs PlayStation)
/// and behavior tuning. Ported from BigScreenLauncher PromptIconTheme equivalence.
/// </summary>
public enum InputDeviceFamily
{
    Auto,
    KeyboardMouse,
    Xbox,
    PlayStation,
}
