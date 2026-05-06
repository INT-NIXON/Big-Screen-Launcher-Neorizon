namespace BSLN.Core.Domain;

public sealed record LaunchTarget(
    LaunchKind Kind,
    string PathOrUri,
    string? Arguments = null,
    string? WorkingDirectory = null);
