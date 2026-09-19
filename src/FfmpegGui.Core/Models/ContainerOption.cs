namespace FfmpegGui.Core.Models;

public sealed class ContainerOption
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Extension { get; init; }
    public required string MuxerName { get; init; }
    public required IReadOnlyList<OutputKind> SupportedKinds { get; init; }
    public required IReadOnlyList<string> VideoCodecIds { get; init; }
    public required IReadOnlyList<string> AudioCodecIds { get; init; }
}
