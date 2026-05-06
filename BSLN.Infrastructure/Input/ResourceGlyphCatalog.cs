using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace BSLN.Infrastructure.Input;

public sealed class ResourceGlyphCatalog : IInputGlyphCatalog
{
    public string GetGlyphPath(InputDeviceFamily inputFamily, SemanticInputAction action)
    {
        var family = inputFamily == InputDeviceFamily.Auto ? InputDeviceFamily.Xbox : inputFamily;
        return (family, action) switch
        {
            (InputDeviceFamily.Xbox, SemanticInputAction.MoveLeft) => "avares://Big_Screen_Launcher_Neorizon/Resources/Xbox%20Series/xbox_dpad.png",
            (InputDeviceFamily.Xbox, SemanticInputAction.Accept) => "avares://Big_Screen_Launcher_Neorizon/Resources/Xbox%20Series/xbox_button_a.png",
            (InputDeviceFamily.Xbox, SemanticInputAction.Back) => "avares://Big_Screen_Launcher_Neorizon/Resources/Xbox%20Series/xbox_button_b.png",
            (InputDeviceFamily.Xbox, SemanticInputAction.Menu) => "avares://Big_Screen_Launcher_Neorizon/Resources/Xbox%20Series/xbox_button_menu.png",
            (InputDeviceFamily.PlayStation, SemanticInputAction.MoveLeft) => "avares://Big_Screen_Launcher_Neorizon/Resources/PlayStation%20Series/playstation_dpad.png",
            (InputDeviceFamily.PlayStation, SemanticInputAction.Accept) => "avares://Big_Screen_Launcher_Neorizon/Resources/PlayStation%20Series/playstation_button_cross.png",
            (InputDeviceFamily.PlayStation, SemanticInputAction.Back) => "avares://Big_Screen_Launcher_Neorizon/Resources/PlayStation%20Series/playstation_button_circle.png",
            (InputDeviceFamily.PlayStation, SemanticInputAction.Menu) => "avares://Big_Screen_Launcher_Neorizon/Resources/PlayStation%20Series/playstation5_button_options.png",
            (InputDeviceFamily.KeyboardMouse, SemanticInputAction.MoveLeft) => "avares://Big_Screen_Launcher_Neorizon/Resources/Keyboard%20%26%20Mouse/keyboard_arrows.png",
            (InputDeviceFamily.KeyboardMouse, SemanticInputAction.Accept) => "avares://Big_Screen_Launcher_Neorizon/Resources/Keyboard%20%26%20Mouse/mouse_left_outline.png",
            (InputDeviceFamily.KeyboardMouse, SemanticInputAction.Back) => "avares://Big_Screen_Launcher_Neorizon/Resources/Keyboard%20%26%20Mouse/keyboard_escape.png",
            (InputDeviceFamily.KeyboardMouse, SemanticInputAction.Menu) => "avares://Big_Screen_Launcher_Neorizon/Resources/Keyboard%20%26%20Mouse/mouse_right_outline.png",
            _ => "avares://Big_Screen_Launcher_Neorizon/Resources/Xbox%20Series/xbox_dpad.png",
        };
    }
}
