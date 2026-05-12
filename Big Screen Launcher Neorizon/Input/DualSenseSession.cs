using System;
using System.Linq;
using System.Threading;
using HidSharp;

namespace Big_Screen_Launcher_Neorizon.Input;

/// <summary>
/// Direct HID backend for DualSense/DualShock controllers.
/// Background thread polls HID at ~60Hz, main thread reads cached snapshot.
/// Report parsing ported exactly from BigScreenLauncher (Rust) dualsense.rs.
///
/// Rust report layout (after slicing):
///   data[0] = Left stick X
///   data[1] = Left stick Y
///   data[2] = Right stick X
///   data[3] = Right stick Y
///   data[4] = Left trigger (analog)
///   data[5] = Right trigger (analog)
///   data[6] = Sequence number
///   data[7] = face bits:  D-pad(4) | Square(4) | Cross(5) | Circle(6) | Triangle(7)
///   data[8] = misc bits:  L1(0) | R1(1) | Share(4) | Start(5) | L3(6) | R3(7)
///   data[9] = special:    PS/Home(0) | Touchpad(1)
///
/// USB: report[0]=0x01, report[1..64] → data[0..]
/// BT:  report[0]=0x31, report[2..65] → data[0..]
/// </summary>
public sealed class DualSenseSession : IDisposable
{
    // ── HID constants ──
    private const int VidSony = 0x054C;
    private const int PidDualSense = 0x0CE6;
    private const int PidDualSenseEdge = 0x0DF2;
    private const int PidDualShock4 = 0x09CC;
    private const int PidDualShock4V2 = 0x0DA0;

    private const int StickScale = 32767;
    private const int ReportMax = 78;

    // ── Background worker ──
    private Thread? _worker;
    private CancellationTokenSource? _cts;

    // ── Cached snapshot (locked for thread safety) ──
    private readonly object _lock = new();
    private Buttons _cachedButtons;
    private short _cachedLx, _cachedLy;
    private bool _cachedConnected;

    // ── Public properties (read from cache) ──
    public bool IsConnected { get { lock (_lock) return _cachedConnected; } }
    public Buttons CurrentButtons { get { lock (_lock) return _cachedButtons; } }
    public short LeftStickX { get { lock (_lock) return _cachedLx; } }
    public short LeftStickY { get { lock (_lock) return _cachedLy; } }

    /// <summary>Reads latest cached state. Non-blocking, safe for UI thread.</summary>
    public void Poll()
    {
        // Lazily start background worker on first poll
        if (_worker is null && _cts is null)
            StartWorker();
    }

    private void StartWorker()
    {
        _cts = new CancellationTokenSource();
        _worker = new Thread(() => WorkerLoop(_cts.Token))
        {
            Name = "DualSense HID",
            IsBackground = true,
        };
        _worker.Start();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _worker = null;
    }

    // ── Background worker loop ──

    private void WorkerLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HidStream? stream = null;

            try
            {
                var device = DeviceList.Local.GetHidDevices(VidSony)
                    .FirstOrDefault(d => d.ProductID is PidDualSense or PidDualSenseEdge
                                             or PidDualShock4 or PidDualShock4V2);

                if (device is null || !device.TryOpen(out stream))
                {
                    SetDisconnected();
                    ct.WaitHandle.WaitOne(500); // retry in 500ms
                    continue;
                }

                stream.ReadTimeout = 16; // ~60Hz
                lock (_lock) _cachedConnected = true;

                var report = new byte[ReportMax];

                while (!ct.IsCancellationRequested)
                {
                    int read;
                    try
                    {
                        read = stream.Read(report, 0, report.Length);
                    }
                    catch (TimeoutException)
                    {
                        continue; // no data this cycle, re-poll
                    }

                    if (read < 10) continue;

                    if (report[0] == 0x01)
                        ParseAndCache(report, dataStart: 1); // USB
                    else if (report[0] == 0x31)
                        ParseAndCache(report, dataStart: 2); // BT (extra HID header byte)
                    // else: unknown report ID, skip
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ThreadInterruptedException) { break; }
            catch (Exception)
            {
                // Device error, cleanup and retry
                stream?.Dispose();
                SetDisconnected();
                ct.WaitHandle.WaitOne(250);
            }
            finally
            {
                stream?.Dispose();
            }
        }
    }

    private void SetDisconnected()
    {
        lock (_lock)
        {
            _cachedConnected = false;
            _cachedButtons = 0;
            _cachedLx = 0;
            _cachedLy = 0;
        }
    }

    // ── Report parsing (exact port from Rust parse_full_state) ──

    private void ParseAndCache(byte[] report, int dataStart)
    {
        // Rust parse_full_state:
        //   data[0] = LX, data[1] = LY, data[7]=face, data[8]=misc, data[9]=special
        //   face: dpad(0-3), Square/X(4), Cross/A(5), Circle/B(6), Triangle/Y(7)
        //   misc: L1(0), R1(1), Share(4), Start(5), L3(6), R3(7)
        //   special: PS/Home(0), Touchpad(1)

        byte lx = report[dataStart + 0];
        byte ly = report[dataStart + 1];
        byte face = report[dataStart + 7];
        byte misc = report[dataStart + 8];
        byte special = report[dataStart + 9];

        var buttons = Buttons.None;

        // ── D-pad ──
        byte dpad = (byte)(face & 0x0F);
        buttons |= dpad switch
        {
            0 => Buttons.DPadUp,
            1 => Buttons.DPadUp | Buttons.DPadRight,
            2 => Buttons.DPadRight,
            3 => Buttons.DPadDown | Buttons.DPadRight,
            4 => Buttons.DPadDown,
            5 => Buttons.DPadDown | Buttons.DPadLeft,
            6 => Buttons.DPadLeft,
            7 => Buttons.DPadUp | Buttons.DPadLeft,
            _ => Buttons.None,
        };

        // ── Face buttons ──
        if ((face & 0x10) != 0) buttons |= Buttons.X;     // Square
        if ((face & 0x20) != 0) buttons |= Buttons.A;     // Cross
        if ((face & 0x40) != 0) buttons |= Buttons.B;     // Circle
        if ((face & 0x80) != 0) buttons |= Buttons.Y;     // Triangle

        // ── Misc buttons ──
        if ((misc & 0x01) != 0) buttons |= Buttons.LeftShoulder;  // L1
        if ((misc & 0x02) != 0) buttons |= Buttons.RightShoulder; // R1
        if ((misc & 0x10) != 0) buttons |= Buttons.Share;
        if ((misc & 0x20) != 0) buttons |= Buttons.Start;
        if ((misc & 0x40) != 0) buttons |= Buttons.LeftThumb;     // L3
        if ((misc & 0x80) != 0) buttons |= Buttons.RightThumb;    // R3

        // ── Special buttons ──
        if ((special & 0x01) != 0) buttons |= Buttons.Guide;      // PS
        if ((special & 0x02) != 0) buttons |= Buttons.Back;       // Touchpad click

        // Note: triggers (analog data[4], data[5]) not mapped to Buttons bitmask.
        // Rust maps them to PageUp/PageDown at the InputAction level, not here.

        // ── Cache snapshot ──
        lock (_lock)
        {
            _cachedButtons = buttons;
            _cachedLx = ScaleStick(lx);
            _cachedLy = ScaleStick(ly);
            _cachedConnected = true;
        }
    }

    private static short ScaleStick(byte raw)
    {
        int centered = raw - 128;
        return (short)Math.Clamp((centered * StickScale) / 127, -StickScale, StickScale);
    }
}
