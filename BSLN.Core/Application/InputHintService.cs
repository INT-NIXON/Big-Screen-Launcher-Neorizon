using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace BSLN.Core.Application;

public sealed class InputHintService(IInputGlyphCatalog glyphCatalog)
{
    public IReadOnlyList<HintDefinition> GetLibraryHints(InputDeviceFamily activeInputFamily)
    {
        var resolvedFamily = activeInputFamily == InputDeviceFamily.Auto
            ? InputDeviceFamily.Xbox
            : activeInputFamily;

        return
        [
            CreateHint(resolvedFamily, SemanticInputAction.MoveLeft, "Navigate"),
            CreateHint(resolvedFamily, SemanticInputAction.Accept, "Launch"),
            CreateHint(resolvedFamily, SemanticInputAction.Back, "Back"),
            CreateHint(resolvedFamily, SemanticInputAction.Menu, "Menu"),
        ];
    }

    private HintDefinition CreateHint(InputDeviceFamily family, SemanticInputAction action, string label)
    {
        return new HintDefinition(action, label, glyphCatalog.GetGlyphPath(family, action));
    }
}
