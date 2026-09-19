namespace FfmpegGui.Core.Models;

public sealed record VideoCodecOption(
    string Id,
    string DisplayName,
    string EncoderName,
    string DefaultPreset = "medium");
