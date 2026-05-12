using System;

namespace Big_Screen_Launcher_Neorizon.Input;

/// <summary>
/// Universal controller button bitmask, ported from BigScreenLauncher (Rust).
/// Single representation used by all input backends (WGI, XInput).
/// </summary>
[Flags]
public enum Buttons : ushort
{
    None = 0,
    DPadUp = 0x0001,
    DPadDown = 0x0002,
    DPadLeft = 0x0004,
    DPadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftThumb = 0x0040,
    RightThumb = 0x0080,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    Guide = 0x0400,
    Share = 0x0800,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000,
}
