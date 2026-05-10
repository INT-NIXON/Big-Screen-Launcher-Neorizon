using System;
using System.Linq;
using Windows.Gaming.Input;
using PCL.Core.Logging;

namespace Big_Screen_Launcher_Neorizon.Services;

/// <summary>WGI-based gamepad. Forced enumeration via static event registration.</summary>
public sealed class GamepadService
{
    private const double DEADZONE = 0.15;

    private static readonly bool _wgiReady;

    static GamepadService()
    {
        try
        {
            // Register events to force WGI enumeration
            Gamepad.GamepadAdded += (_, _) => { };
            Gamepad.GamepadRemoved += (_, _) => { };
            _wgiReady = true;
        }
        catch { _wgiReady = false; }
    }

    private bool _prevConnected;
    private GamepadButtons _prevButtons;
    private DirectionState _lr = new(), _ud = new();

    public event Action<SemanticInputAction>? ActionReceived;
    public event Action<bool>? ConnectionChanged;
    public bool IsConnected { get; private set; }

    public void Poll()
    {
        try
        {
            if (!_wgiReady) return;

            var gamepad = Gamepad.Gamepads.FirstOrDefault();
            bool connected = gamepad != null;
            IsConnected = connected;

            if (connected != _prevConnected)
            {
                _prevConnected = connected;
                ConnectionChanged?.Invoke(connected);
            }

            if (!connected)
            {
                _prevButtons = 0;
                _lr.Reset(); _ud.Reset();
                return;
            }

            var r = gamepad.GetCurrentReading();
            var b = r.Buttons;

            // Edge detection
            var pressed = b & ~_prevButtons;
            if ((pressed & GamepadButtons.A) != 0) Fire(SemanticInputAction.Accept);
            if ((pressed & GamepadButtons.B) != 0) Fire(SemanticInputAction.Back);
            if ((pressed & GamepadButtons.Menu) != 0) Fire(SemanticInputAction.Menu);
            if ((pressed & GamepadButtons.LeftShoulder) != 0) Fire(SemanticInputAction.PageLeft);
            if ((pressed & GamepadButtons.RightShoulder) != 0) Fire(SemanticInputAction.PageRight);
            _prevButtons = b;

            /* D-pad + thumbstick */
            bool left = (b & GamepadButtons.DPadLeft) != 0 || r.LeftThumbstickX < -DEADZONE;
            bool right = (b & GamepadButtons.DPadRight) != 0 || r.LeftThumbstickX > DEADZONE;
            bool up = (b & GamepadButtons.DPadUp) != 0 || r.LeftThumbstickY > DEADZONE;
            bool down = (b & GamepadButtons.DPadDown) != 0 || r.LeftThumbstickY < -DEADZONE;

            HandleRepeat(ref _lr, left, right, SemanticInputAction.MoveLeft, SemanticInputAction.MoveRight);
            HandleRepeat(ref _ud, up, down, SemanticInputAction.MoveUp, SemanticInputAction.MoveDown);
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Gamepad poll error");
        }
    }

    private void Fire(SemanticInputAction a) => ActionReceived?.Invoke(a);

    private void HandleRepeat(ref DirectionState ds, bool pos, bool neg,
                              SemanticInputAction posA, SemanticInputAction negA)
    {
        bool active = pos || neg;
        if (!active) { ds.Reset(); return; }
        var now = DateTime.UtcNow;
        if (!ds.WasActive)
        {
            if (pos) Fire(posA); if (neg) Fire(negA);
            ds.WasActive = true; ds.LastFire = now; ds.IsFirst = true;
        }
        else
        {
            double d = ds.IsFirst ? 350 : 120;
            if ((now - ds.LastFire).TotalMilliseconds >= d)
            {
                if (pos) Fire(posA); if (neg) Fire(negA);
                ds.LastFire = now; ds.IsFirst = false;
            }
        }
    }

    private struct DirectionState
    {
        public bool WasActive, IsFirst;
        public DateTime LastFire;
        public void Reset() { WasActive = false; IsFirst = false; }
    }
}
