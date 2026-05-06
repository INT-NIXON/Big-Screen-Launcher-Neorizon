using BSLN.Core.Domain;

namespace BSLN.Core.Application.Abstractions;

public interface IInputGlyphCatalog
{
    string GetGlyphPath(InputDeviceFamily inputFamily, SemanticInputAction action);
}
