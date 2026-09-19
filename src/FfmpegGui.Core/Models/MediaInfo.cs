using System.Globalization;

namespace FfmpegGui.Core.Models;

public sealed class MediaInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string FormatName { get; init; } = string.Empty;
    public string FormatLongName { get; init; } = string.Empty;
    public TimeSpan? Duration { get; init; }
    public long? SizeBytes { get; init; }
    public long? BitRate { get; init; }
    public IReadOnlyList<MediaStreamInfo> Streams { get; init; } = Array.Empty<MediaStreamInfo>();

    public string FileName => Path.GetFileName(FilePath);

    public string FormatDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(FormatLongName)) return FormatLongName;
            return !string.IsNullOrWhiteSpace(FormatName) ? FormatName : "未知";
        }
    }

    public string DurationDisplay => Duration is { } duration
        ? duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
        : "未知";

    public string SizeDisplay => SizeBytes is { } size ? FormatSize(size) : "未知";

    public string BitRateDisplay => BitRate is { } bitRate
        ? $"{bitRate / 1000.0:0.#} kbps"
        : "未知";

    public IReadOnlyList<MediaStreamInfo> VideoStreams =>
        Streams.Where(stream => stream.StreamType == MediaStreamType.Video).ToList();

    public IReadOnlyList<MediaStreamInfo> AudioStreams =>
        Streams.Where(stream => stream.StreamType == MediaStreamType.Audio).ToList();

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }
}
