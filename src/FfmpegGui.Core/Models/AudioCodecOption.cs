namespace FfmpegGui.Core.Models;

public sealed record AudioCodecOption(
    string Id,
    string DisplayName,
    string EncoderName,
    bool IsLossy,
    int DefaultBitrateKbps);
