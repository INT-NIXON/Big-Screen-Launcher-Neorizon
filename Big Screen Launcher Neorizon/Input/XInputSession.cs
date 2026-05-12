using System;
using System.Runtime.InteropServices;

namespace Big_Screen_Launcher_Neorizon.Input;

/// <summary>
/// XInput P/Invoke polling backend (fallback for legacy Xbox controllers).
/// Pure state reader: maps XINPUT_STATE → Buttons bitmask.
/// No repeat logic, no events — just snapshot.
/// </summary>
public sealed class XInputSession
{
    private const int ErrorDeviceNotConnected = 0x48F;

    // XInput button bitmask constants matching Buttons enum
    private const ushort XI_DPAD_UP = 0x0001;
    private const ushort XI_DPAD_DOWN = 0x0002;
    private const ushort XI_DPAD_LEFT = 0x0004;
    private const ushort XI_DPAD_RIGHT = 0x0008;
    private const ushort XI_START = 0x0010;
    private const ushort XI_BACK = 0x0020;
    private const ushort XI_LEFT_THUMB = 0x0040;
    private const ushort XI_RIGHT_THUMB = 0x0080;
    private const ushort XI_LEFT_SHOULDER = 0x0100;
    private const ushort XI_RIGHT_SHOULDER = 0x0200;
    private const ushort XI_GUIDE = 0x0400;
    private const ushort XI_A = 0x1000;
    private const ushort XI_B = 0x2000;
    private const ushort XI_X = 0x4000;
    private const ushort XI_Y = 0x8000;

    public bool IsConnected { get; private set; }
    public Buttons CurrentButtons { get; private set; }
    public short LeftStickX { get; private set; }
    public short LeftStickY { get; private set; }

    public void Poll()
    {
        try
        {
            var state = new XInputState();
            int result = XInputGetState(0, ref state);

            if (result == ErrorDeviceNotConnected)
            {
                IsConnected = false;
                CurrentButtons = 0;
                return;
            }

            IsConnected = true;
            CurrentButtons = MapButtons(state.Gamepad.wButtons);
            LeftStickX = state.Gamepad.sThumbLX;
            LeftStickY = state.Gamepad.sThumbLY;
        }
        catch
        {
            IsConnected = false;
            CurrentButtons = 0;
        }
    }

    private static Buttons MapButtons(ushort xi)
    {
        var b = Buttons.None;
        if ((xi & XI_DPAD_UP) != 0) b |= Buttons.DPadUp;
        if ((xi & XI_DPAD_DOWN) != 0) b |= Buttons.DPadDown;
        if ((xi & XI_DPAD_LEFT) != 0) b |= Buttons.DPadLeft;
        if ((xi & XI_DPAD_RIGHT) != 0) b |= Buttons.DPadRight;
        if ((xi & XI_START) != 0) b |= Buttons.Start;
        if ((xi & XI_BACK) != 0) b |= Buttons.Back;
        if ((xi & XI_LEFT_THUMB) != 0) b |= Buttons.LeftThumb;
        if ((xi & XI_RIGHT_THUMB) != 0) b |= Buttons.RightThumb;
        if ((xi & XI_LEFT_SHOULDER) != 0) b |= Buttons.LeftShoulder;
        if ((xi & XI_RIGHT_SHOULDER) != 0) b |= Buttons.RightShoulder;
        if ((xi & XI_GUIDE) != 0) b |= Buttons.Guide;
        if ((xi & XI_A) != 0) b |= Buttons.A;
        if ((xi & XI_B) != 0) b |= Buttons.B;
        if ((xi & XI_X) != 0) b |= Buttons.X;
        if ((xi & XI_Y) != 0) b |= Buttons.Y;
        return b;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState(int dwUserIndex, ref XInputState pState);

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public int dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }
}
