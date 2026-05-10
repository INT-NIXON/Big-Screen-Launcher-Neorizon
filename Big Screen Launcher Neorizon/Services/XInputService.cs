using System;
using System.Runtime.InteropServices;
using PCL.Core.Logging;

namespace Big_Screen_Launcher_Neorizon.Services;

public sealed class XInputService
{
    private const int ERROR_DEVICE_NOT_CONNECTED = 0x48F;

    // XInput button bitmask constants
    private const ushort A_BUTTON = 0x1000;
    private const ushort B_BUTTON = 0x2000;
    private const ushort DPAD_UP = 0x0001;
    private const ushort DPAD_DOWN = 0x0002;
    private const ushort DPAD_LEFT = 0x0004;
    private const ushort DPAD_RIGHT = 0x0008;
    private const ushort START = 0x0010;
    private const ushort LEFT_SHOULDER = 0x0100;
    private const ushort RIGHT_SHOULDER = 0x0200;

    private const int THUMBSTICK_THRESHOLD = 12000;

    private ushort _prevButtons;
    private bool _prevConnected;
    private DirectionState _leftRight = new();
    private DirectionState _upDown = new();

    public event Action<SemanticInputAction>? ActionReceived;
    public event Action<bool>? ConnectionChanged;
    public bool IsConnected { get; private set; }

    public void PollNow()
    {
        try
        {
            // Use raw memory for XInputGetState to avoid struct marshalling issues
            IntPtr ptr = Marshal.AllocHGlobal(16);
            try
            {
                // Zero out memory
                for (int i = 0; i < 4; i++)
                    Marshal.WriteInt32(ptr, i * 4, 0);

                int result = NativeXInputGetState(0, ptr);

                bool connected = result != ERROR_DEVICE_NOT_CONNECTED;
                IsConnected = connected;

                if (connected != _prevConnected)
                {
                    _prevConnected = connected;
                    ConnectionChanged?.Invoke(connected);
                }

                if (!connected)
                {
                    _prevButtons = 0;
                    _leftRight.Reset();
                    _upDown.Reset();
                    return;
                }

                // Read raw data from native memory at correct offsets
                ushort buttons = (ushort)Marshal.ReadInt16(ptr, 4);
                short thumbLX = Marshal.ReadInt16(ptr, 8);
                short thumbLY = Marshal.ReadInt16(ptr, 10);

                if (buttons != 0)
                    LogWrapper.Info($"XInput buttons: 0x{buttons:X4}");

                // Edge detection: buttons that are pressed now but weren't before
                ushort pressed = (ushort)(buttons & ~_prevButtons);

                if ((pressed & A_BUTTON) != 0) Fire(SemanticInputAction.Accept);
                if ((pressed & B_BUTTON) != 0) Fire(SemanticInputAction.Back);
                if ((pressed & START) != 0) Fire(SemanticInputAction.Menu);
                if ((pressed & LEFT_SHOULDER) != 0) Fire(SemanticInputAction.PageLeft);
                if ((pressed & RIGHT_SHOULDER) != 0) Fire(SemanticInputAction.PageRight);

                _prevButtons = buttons;

                // D-pad + thumbstick → directional actions
                bool left = (buttons & DPAD_LEFT) != 0 || thumbLX < -THUMBSTICK_THRESHOLD;
                bool right = (buttons & DPAD_RIGHT) != 0 || thumbLX > THUMBSTICK_THRESHOLD;
                bool up = (buttons & DPAD_UP) != 0 || thumbLY > THUMBSTICK_THRESHOLD;
                bool down = (buttons & DPAD_DOWN) != 0 || thumbLY < -THUMBSTICK_THRESHOLD;

                HandleRepeat(ref _leftRight, left, right, SemanticInputAction.MoveLeft, SemanticInputAction.MoveRight);
                HandleRepeat(ref _upDown, up, down, SemanticInputAction.MoveUp, SemanticInputAction.MoveDown);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "XInput poll error");
        }
    }

    private void HandleRepeat(ref DirectionState ds, bool pos, bool neg,
                              SemanticInputAction posAct, SemanticInputAction negAct)
    {
        bool active = pos || neg;
        if (!active) { ds.Reset(); return; }

        var now = DateTime.UtcNow;

        if (!ds.WasActive)
        {
            if (pos) Fire(posAct);
            if (neg) Fire(negAct);
            ds.WasActive = true;
            ds.LastFireTime = now;
            ds.InitialDelay = true;
        }
        else
        {
            double delay = ds.InitialDelay ? 350 : 120;
            if ((now - ds.LastFireTime).TotalMilliseconds >= delay)
            {
                if (pos) Fire(posAct);
                if (neg) Fire(negAct);
                ds.LastFireTime = now;
                ds.InitialDelay = false;
            }
        }
    }

    private void Fire(SemanticInputAction action) => ActionReceived?.Invoke(action);

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern int NativeXInputGetState(int dwUserIndex, IntPtr pState);

    private struct DirectionState
    {
        public bool WasActive, InitialDelay;
        public DateTime LastFireTime;
        public void Reset() { WasActive = false; InitialDelay = false; }
    }
}
