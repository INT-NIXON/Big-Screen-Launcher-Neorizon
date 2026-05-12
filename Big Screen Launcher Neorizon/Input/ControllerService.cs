using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Big_Screen_Launcher_Neorizon.Input;

/// <summary>
/// Unified controller service. Ported architecture from BigScreenLauncher (Rust):
///   • Aggregates WGI + DualSense HID + XInput backends into single Buttons state
///   • Edge detection for button → ControllerAction mapping
///   • Nav repeat with acceleration (initial 350ms → 120ms → 45ms)
///   • InputDeviceFamily tracking (PlayStation vs Xbox)
///   • Events for actions and family changes
/// </summary>
public sealed class ControllerService
{
    // ── Repeat timing constants (ported from Rust reference) ──
    private const long InitialPressWindowMs = 350;
    private const long RepeatStage1Ms = 120;   // 350-1300ms hold
    private const long RepeatStage2Ms = 45;    // 1300ms+ hold
    private const long StageBoundaryMs = 1300;

    // ── Stick threshold ──
    private const int StickThreshold = 12000; // ~0.366 * 32767, matches existing XInputService

    // ── Backend sessions (priority: WGI → DualSense HID → XInput) ──
    private readonly WgiSession _wgi = new();
    private readonly DualSenseSession _ds = new();
    private readonly XInputSession _xinput = new();

    // ── State tracking ──
    private Buttons _prevButtons;
    private readonly Dictionary<ControllerAction, RepeatState> _repeat = new();
    private InputDeviceFamily _activeFamily = InputDeviceFamily.Auto;
    private InputDeviceFamily _lastActiveFamily = InputDeviceFamily.Auto;
    private bool _familyInitialized;

    // ── Events ──
    public event Action<ControllerAction>? ActionReceived;
    public event Action<InputDeviceFamily>? FamilyChanged;

    // ── Properties ──
    public InputDeviceFamily ActiveFamily => _activeFamily;
    public bool IsAnyConnected => _wgi.IsConnected || _ds.IsConnected || _xinput.IsConnected;
    public WgiSession Wgi => _wgi;
    public XInputSession Xinput => _xinput;

    /// <summary>Call at ~30fps from UI thread. Does all polling + dispatching.</summary>
    public void Poll()
    {
        _wgi.Poll();
        _ds.Poll();
        _xinput.Poll();

        var now = DateTime.UtcNow;

        // ── Track last-active controller for icon switching ──
        if (_wgi.IsConnected)
        {
            if (_wgi.CurrentButtons != 0)
                _lastActiveFamily = _wgi.IsDualSense ? InputDeviceFamily.PlayStation : InputDeviceFamily.Xbox;
        }
        if (_ds.IsConnected && _ds.CurrentButtons != 0)
            _lastActiveFamily = InputDeviceFamily.PlayStation;
        if (_xinput.IsConnected && _xinput.CurrentButtons != 0)
            _lastActiveFamily = InputDeviceFamily.Xbox;

        // ── Aggregate: OR all connected backends (Rust pattern: combine all) ──
        Buttons current = Buttons.None;
        if (_wgi.IsConnected) current |= _wgi.CurrentButtons;
        if (_ds.IsConnected) current |= _ds.CurrentButtons;
        if (_xinput.IsConnected) current |= _xinput.CurrentButtons;

        // Sticks: prefer the backend with actual stick activity, else first connected
        PickStick(out short lx, out short ly);

        // ── Track device family ──
        UpdateDeviceFamily();

        // ── Edge-detect new presses ──
        Buttons pressed = current & ~_prevButtons;

        // Map newly pressed buttons to actions (no repeats for action buttons)
        foreach (var action in MapButtonsToActions(pressed))
        {
            FireAction(action);
            StartRepeat(action, now);
        }

        // Clear repeat state for released buttons
        var released = _prevButtons & ~current;
        foreach (var action in MapButtonsToActions(released))
            _repeat.Remove(action);

        // ── Directional from stick ──
        bool stickLeft = lx < -StickThreshold;
        bool stickRight = lx > StickThreshold;
        bool stickUp = ly > StickThreshold;
        bool stickDown = ly < -StickThreshold;

        // D-pad bits
        bool dpadLeft = (current & Buttons.DPadLeft) != 0;
        bool dpadRight = (current & Buttons.DPadRight) != 0;
        bool dpadUp = (current & Buttons.DPadUp) != 0;
        bool dpadDown = (current & Buttons.DPadDown) != 0;

        // Combined: stick OR dpad
        bool wantLeft = stickLeft || dpadLeft;
        bool wantRight = stickRight || dpadRight;
        bool wantUp = stickUp || dpadUp;
        bool wantDown = stickDown || dpadDown;

        HandleDirection(ControllerAction.MoveLeft, wantLeft, now);
        HandleDirection(ControllerAction.MoveRight, wantRight, now);
        HandleDirection(ControllerAction.MoveUp, wantUp, now);
        HandleDirection(ControllerAction.MoveDown, wantDown, now);

        _prevButtons = current;
    }

    // ── Button → action mapping ──

    private static IEnumerable<ControllerAction> MapButtonsToActions(Buttons buttons)
    {
        if (buttons == Buttons.None) yield break;

        if ((buttons & Buttons.A) != 0) yield return ControllerAction.Accept;
        if ((buttons & Buttons.B) != 0) yield return ControllerAction.Back;
        if ((buttons & Buttons.X) != 0) yield return ControllerAction.Refresh;
        if ((buttons & Buttons.Y) != 0) yield return ControllerAction.Settings;
        if ((buttons & Buttons.Start) != 0) yield return ControllerAction.Menu;
        if ((buttons & Buttons.LeftShoulder) != 0) yield return ControllerAction.PageUp;
        if ((buttons & Buttons.RightShoulder) != 0) yield return ControllerAction.PageDown;
    }

    // ── Direction repeat logic (ported from Rust) ──

    private void HandleDirection(ControllerAction action, bool active, DateTime now)
    {
        if (!active)
        {
            _repeat.Remove(action);
            return;
        }

        if (_repeat.TryGetValue(action, out var state))
        {
            // Repeat check
            double elapsed = (now - state.Activated).TotalMilliseconds;
            if (elapsed < InitialPressWindowMs) return; // still in initial delay window

            double interval = elapsed < StageBoundaryMs ? RepeatStage1Ms : RepeatStage2Ms;
            if ((now - state.LastFired).TotalMilliseconds >= interval)
            {
                FireAction(action);
                _repeat[action] = state with { LastFired = now };
            }
        }
        else
        {
            // First press — fire immediately
            FireAction(action);
            _repeat[action] = new RepeatState { Activated = now, LastFired = now };
        }
    }

    private void StartRepeat(ControllerAction action, DateTime now)
    {
        // Only track repeatable actions
        if (action is ControllerAction.MoveLeft or ControllerAction.MoveRight
            or ControllerAction.MoveUp or ControllerAction.MoveDown
            or ControllerAction.PageUp or ControllerAction.PageDown)
        {
            if (!_repeat.ContainsKey(action))
                _repeat[action] = new RepeatState { Activated = now, LastFired = now };
        }
    }

    // ── Device family detection ──

    private void UpdateDeviceFamily()
    {
        InputDeviceFamily detected;

        // Prefer last-active controller (user just pressed a button on it)
        if (_lastActiveFamily != InputDeviceFamily.Auto)
        {
            detected = _lastActiveFamily;
        }
        else if (_ds.IsConnected || _wgi.IsDualSense)
        {
            detected = InputDeviceFamily.PlayStation;
        }
        else if (_wgi.IsConnected || _xinput.IsConnected)
        {
            detected = InputDeviceFamily.Xbox;
        }
        else
        {
            detected = DetectPlayStationFromRegistry() ? InputDeviceFamily.PlayStation
                     : InputDeviceFamily.Auto;
        }

        if (detected != _activeFamily || !_familyInitialized)
        {
            _activeFamily = detected;
            _familyInitialized = true;
            FamilyChanged?.Invoke(detected);
        }
    }

    // ── Stick aggregation: prefer backend with actual stick movement ──

    private void PickStick(out short lx, out short ly)
    {
        // Check in priority order, use first with stick beyond deadzone
        if (_wgi.IsConnected && (Math.Abs(_wgi.LeftStickX) > StickThreshold || Math.Abs(_wgi.LeftStickY) > StickThreshold))
        { lx = _wgi.LeftStickX; ly = _wgi.LeftStickY; return; }
        if (_ds.IsConnected && (Math.Abs(_ds.LeftStickX) > StickThreshold || Math.Abs(_ds.LeftStickY) > StickThreshold))
        { lx = _ds.LeftStickX; ly = _ds.LeftStickY; return; }
        if (_xinput.IsConnected && (Math.Abs(_xinput.LeftStickX) > StickThreshold || Math.Abs(_xinput.LeftStickY) > StickThreshold))
        { lx = _xinput.LeftStickX; ly = _xinput.LeftStickY; return; }

        // No activity: use first connected
        if (_wgi.IsConnected) { lx = _wgi.LeftStickX; ly = _wgi.LeftStickY; return; }
        if (_ds.IsConnected) { lx = _ds.LeftStickX; ly = _ds.LeftStickY; return; }
        if (_xinput.IsConnected) { lx = _xinput.LeftStickX; ly = _xinput.LeftStickY; return; }

        lx = 0; ly = 0;
    }

    private static bool DetectPlayStationFromRegistry()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Enum\HID");
            if (baseKey is null) return false;

            string[] psPrefixes =
            [
                "VID_054C&PID_0CE6", // DualSense
                "VID_054C&PID_0DF2", // DualSense Edge
                "VID_054C&PID_09CC", // DualShock 4
                "VID_054C&PID_0DA0", // DualShock 4 v2
                "VID_054C&PID_0BA0", // DualShock 3
            ];

            foreach (var name in baseKey.GetSubKeyNames())
                foreach (var prefix in psPrefixes)
                    if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return true;
        }
        catch
        {
            // ignore
        }
        return false;
    }

    private void FireAction(ControllerAction action) => ActionReceived?.Invoke(action);

    // ── Repeat state ──

    private record struct RepeatState
    {
        public DateTime Activated;
        public DateTime LastFired;
    }
}
