using System.Runtime.InteropServices;
using System.Timers;
using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;
using Timer = System.Timers.Timer;

namespace BSLN.Core.Input;

public sealed class XInputPollingService : IControllerInputSource
{
    private readonly Timer _timer;
    private readonly ControllerActionTranslator _translator;
    private ControllerSnapshot _previousSnapshot = new(false, false, false, false, false, false, false, false, false, false, false, 0, 0);

    public XInputPollingService(ControllerActionTranslator translator)
    {
        _translator = translator;
        _timer = new Timer(100);
        _timer.Elapsed += OnElapsed;
    }

    public event EventHandler<SemanticInputAction>? ActionReceived;
    public event EventHandler<InputDeviceFamily>? InputFamilyChanged;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    private void OnElapsed(object? sender, ElapsedEventArgs e)
    {
        var current = TryReadSnapshot();
        if (!current.IsConnected)
        {
            _previousSnapshot = current;
            return;
        }

        var actions = _translator.Translate(_previousSnapshot, current);
        if (actions.Count > 0)
        {
            InputFamilyChanged?.Invoke(this, InputDeviceFamily.Xbox);
            foreach (var action in actions)
            {
                ActionReceived?.Invoke(this, action);
            }
        }

        _previousSnapshot = current;
    }

    private static ControllerSnapshot TryReadSnapshot()
    {
        try
        {
            var state = new XInputState();
            var result = XInputGetState(0, ref state);
            if (result != 0)
            {
                return new ControllerSnapshot(false, false, false, false, false, false, false, false, false, false, false, 0, 0);
            }

            return new ControllerSnapshot(
                IsConnected: true,
                DPadLeft: (state.Gamepad.wButtons & 0x0004) != 0,
                DPadRight: (state.Gamepad.wButtons & 0x0008) != 0,
                DPadUp: (state.Gamepad.wButtons & 0x0001) != 0,
                DPadDown: (state.Gamepad.wButtons & 0x0002) != 0,
                ButtonSouth: (state.Gamepad.wButtons & 0x1000) != 0,
                ButtonEast: (state.Gamepad.wButtons & 0x2000) != 0,
                ButtonMenu: (state.Gamepad.wButtons & 0x0010) != 0,
                ButtonGuide: (state.Gamepad.wButtons & 0x0400) != 0,
                LeftShoulder: (state.Gamepad.wButtons & 0x0100) != 0,
                RightShoulder: (state.Gamepad.wButtons & 0x0200) != 0,
                LeftThumbX: state.Gamepad.sThumbLX,
                LeftThumbY: state.Gamepad.sThumbLY);
        }
        catch
        {
            return new ControllerSnapshot(false, false, false, false, false, false, false, false, false, false, false, 0, 0);
        }
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
