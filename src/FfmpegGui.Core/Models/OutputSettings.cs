namespace FfmpegGui.Core.Models;

public sealed class OutputSettings
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public required OutputKind OutputKind { get; init; }
    public required ContainerOption Container { get; init; }
    public int VideoStreamOrdinal { get; init; }
    public int AudioStreamOrdinal { get; init; }
    public VideoCodecOption? VideoCodec { get; init; }
    public AudioCodecOption? AudioCodec { get; init; }
    public VideoQualityOption? VideoQuality { get; init; }
    public AudioBitrateOption? AudioBitrate { get; init; }
}

