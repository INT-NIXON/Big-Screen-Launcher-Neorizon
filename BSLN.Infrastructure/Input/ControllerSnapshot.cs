namespace BSLN.Infrastructure.Input;

public sealed record ControllerSnapshot(
    bool IsConnected,
    bool DPadLeft,
    bool DPadRight,
    bool DPadUp,
    bool DPadDown,
    bool ButtonSouth,
    bool ButtonEast,
    bool ButtonMenu,
    bool ButtonGuide,
    bool LeftShoulder,
    bool RightShoulder,
    short LeftThumbX,
    short LeftThumbY);
