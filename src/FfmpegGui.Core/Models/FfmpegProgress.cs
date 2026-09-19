namespace FfmpegGui.Core.Models;

public sealed record FfmpegProgress(
    TimeSpan? ProcessedDuration,
    double? Percentage,
    double? Speed,
    long? TotalSizeBytes,
    string State);
