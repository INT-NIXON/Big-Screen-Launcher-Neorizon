using BSLN.Core.Domain;

namespace BSLN.Core.Application;

public sealed record HintDefinition(
    SemanticInputAction Action,
    string Label,
    string GlyphPath);
