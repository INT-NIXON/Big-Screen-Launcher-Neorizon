using System;
using System.Linq;
using Windows.Gaming.Input;

namespace Big_Screen_Launcher_Neorizon.Input;

/// <summary>
/// Windows.Gaming.Input polling backend. Handles Xbox + DualSense via WinRT.
/// Pure state reader: maps GamepadReading → Buttons bitmask.
/// No repeat logic, no events — just snapshot.
/// </summary>
public sealed class WgiSession
{
    private static readonly bool _wgiReady;

    static WgiSession()
    {
        try
        {
            Gamepad.GamepadAdded += (_, _) => { };
            Gamepad.GamepadRemoved += (_, _) => { };
            _wgiReady = true;
        }
        catch
        {
            _wgiReady = false;
        }
    }

    public bool IsConnected { get; private set; }
    public Buttons CurrentButtons { get; private set; }

    /// <summary>Left stick X, scaled to [-32768, 32767] matching XInput convention.</summary>
    public short LeftStickX { get; private set; }

    /// <summary>Left stick Y, positive = up.</summary>
    public short LeftStickY { get; private set; }

    /// <summary>True when the connected gamepad is a DualSense (Win11+).</summary>
    public bool IsDualSense { get; private set; }

    public void Poll()
    {
        try
        {
            if (!_wgiReady)
            {
                IsConnected = false;
                return;
            }

            var gamepad = Gamepad.Gamepads.FirstOrDefault();
            if (gamepad is null)
            {
                IsConnected = false;
                CurrentButtons = 0;
                return;
            }

            IsConnected = true;
            var reading = gamepad.GetCurrentReading();
            CurrentButtons = MapButtons(reading.Buttons);
            LeftStickX = (short)(reading.LeftThumbstickX * 32767);
            LeftStickY = (short)(reading.LeftThumbstickY * 32767);

            // Detect DualSense via runtime type check (DualSenseGamepad added in Win11 22H2 SDK)
            if (!IsDualSense)
                IsDualSense = IsDualSenseGamepad(gamepad);
        }
        catch
        {
            IsConnected = false;
            CurrentButtons = 0;
        }
    }

    private static Buttons MapButtons(GamepadButtons wgi)
    {
        var b = Buttons.None;
        if ((wgi & GamepadButtons.DPadUp) != 0) b |= Buttons.DPadUp;
        if ((wgi & GamepadButtons.DPadDown) != 0) b |= Buttons.DPadDown;
        if ((wgi & GamepadButtons.DPadLeft) != 0) b |= Buttons.DPadLeft;
        if ((wgi & GamepadButtons.DPadRight) != 0) b |= Buttons.DPadRight;
        if ((wgi & GamepadButtons.Menu) != 0) b |= Buttons.Start;
        if ((wgi & GamepadButtons.View) != 0) b |= Buttons.Back;
        if ((wgi & GamepadButtons.LeftThumbstick) != 0) b |= Buttons.LeftThumb;
        if ((wgi & GamepadButtons.RightThumbstick) != 0) b |= Buttons.RightThumb;
        if ((wgi & GamepadButtons.LeftShoulder) != 0) b |= Buttons.LeftShoulder;
        if ((wgi & GamepadButtons.RightShoulder) != 0) b |= Buttons.RightShoulder;
        if ((wgi & GamepadButtons.A) != 0) b |= Buttons.A;
        if ((wgi & GamepadButtons.B) != 0) b |= Buttons.B;
        if ((wgi & GamepadButtons.X) != 0) b |= Buttons.X;
        if ((wgi & GamepadButtons.Y) != 0) b |= Buttons.Y;
        // Note: Guide/PS button not exposed through WGI GamepadButtons
        return b;
    }

    private static bool IsDualSenseGamepad(Gamepad gamepad)
    {
        try
        {
            var dsType = Type.GetType(
                "Windows.Gaming.Input.DualSenseGamepad, Windows.Gaming.Input", false);
            return dsType is not null && dsType.IsInstanceOfType(gamepad);
        }
        catch
        {
            return false;
        }
    }
}
