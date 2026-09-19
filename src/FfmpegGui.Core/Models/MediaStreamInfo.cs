namespace FfmpegGui.Core.Models;

public enum MediaStreamType
{
    Video,
    Audio,
    Subtitle,
    Data,
    Attachment,
    Unknown
}

public sealed class MediaStreamInfo
{
    public int Index { get; init; }
    public MediaStreamType StreamType { get; init; }
    public string CodecName { get; init; } = string.Empty;
    public string CodecLongName { get; init; } = string.Empty;
    public string? Profile { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public string? PixelFormat { get; init; }
    public double? FrameRate { get; init; }
    public long? BitRate { get; init; }
    public int? SampleRate { get; init; }
    public int? Channels { get; init; }
    public string? ChannelLayout { get; init; }
    public double? DurationSeconds { get; init; }
    public string? Language { get; init; }
    public string? Title { get; init; }
    public bool IsDefault { get; init; }
    public bool IsForced { get; init; }

    public string StreamTypeText => StreamType switch
    {
        MediaStreamType.Video => "视频",
        MediaStreamType.Audio => "音频",
        MediaStreamType.Subtitle => "字幕",
        MediaStreamType.Data => "数据",
        MediaStreamType.Attachment => "附件",
        _ => "未知"
    };

    public string CodecDisplay => !string.IsNullOrWhiteSpace(CodecLongName)
        ? CodecLongName
        : !string.IsNullOrWhiteSpace(CodecName) ? CodecName : "未知";

    public string ResolutionText => Width is > 0 && Height is > 0 ? $"{Width}x{Height}" : "-";

    public string FrameRateText => FrameRate is { } frameRate ? $"{frameRate:0.###} fps" : "-";

    public string BitRateText => BitRate is { } bitRate ? $"{bitRate / 1000.0:0.#} kbps" : "-";

    public string SampleRateText => SampleRate is { } sampleRate ? $"{sampleRate} Hz" : "-";

    public string ChannelsText => Channels is { } channels ? $"{channels} 声道" : "-";

    public string LanguageText => string.IsNullOrWhiteSpace(Language) ? "-" : Language;

    public string TitleText => string.IsNullOrWhiteSpace(Title) ? "-" : Title;

    public string DefaultText => IsDefault ? "是" : "否";

    public string ForcedText => IsForced ? "是" : "否";
}
