using FfmpegGui.Core.Models;

namespace FfmpegGui.Core.Services;

public static class MediaFormatCatalog
{
    public static IReadOnlyList<ContainerOption> Containers { get; } =
    [
        new ContainerOption
        {
            Id = "mp4",
            DisplayName = "MP4 (.mp4)",
            Extension = "mp4",
            MuxerName = "mp4",
            SupportedKinds = [OutputKind.VideoAndAudio, OutputKind.VideoOnly],
            VideoCodecIds = ["h264", "hevc"],
            AudioCodecIds = ["aac", "mp3"]
        },
        new ContainerOption
        {
            Id = "mkv",
            DisplayName = "MKV (.mkv)",
            Extension = "mkv",
            MuxerName = "matroska",
            SupportedKinds = [OutputKind.VideoAndAudio, OutputKind.VideoOnly, OutputKind.AudioOnly],
            VideoCodecIds = ["h264", "hevc", "vp9"],
            AudioCodecIds = ["aac", "mp3", "opus", "vorbis", "flac", "pcm"]
        },
        new ContainerOption
        {
            Id = "webm",
            DisplayName = "WebM (.webm)",
            Extension = "webm",
            MuxerName = "webm",
            SupportedKinds = [OutputKind.VideoAndAudio, OutputKind.VideoOnly, OutputKind.AudioOnly],
            VideoCodecIds = ["vp9"],
            AudioCodecIds = ["opus", "vorbis"]
        },
        new ContainerOption
        {
            Id = "mov",
            DisplayName = "MOV (.mov)",
            Extension = "mov",
            MuxerName = "mov",
            SupportedKinds = [OutputKind.VideoAndAudio, OutputKind.VideoOnly],
            VideoCodecIds = ["h264", "hevc"],
            AudioCodecIds = ["aac", "mp3", "pcm"]
        },
        new ContainerOption
        {
            Id = "m4a",
            DisplayName = "M4A (.m4a)",
            Extension = "m4a",
            MuxerName = "ipod",
            SupportedKinds = [OutputKind.AudioOnly],
            VideoCodecIds = [],
            AudioCodecIds = ["aac"]
        },
        new ContainerOption
        {
            Id = "mp3",
            DisplayName = "MP3 (.mp3)",
            Extension = "mp3",
            MuxerName = "mp3",
            SupportedKinds = [OutputKind.AudioOnly],
            VideoCodecIds = [],
            AudioCodecIds = ["mp3"]
        },
        new ContainerOption
        {
            Id = "flac",
            DisplayName = "FLAC (.flac)",
            Extension = "flac",
            MuxerName = "flac",
            SupportedKinds = [OutputKind.AudioOnly],
            VideoCodecIds = [],
            AudioCodecIds = ["flac"]
        },
        new ContainerOption
        {
            Id = "wav",
            DisplayName = "WAV (.wav)",
            Extension = "wav",
            MuxerName = "wav",
            SupportedKinds = [OutputKind.AudioOnly],
            VideoCodecIds = [],
            AudioCodecIds = ["pcm"]
        },
        new ContainerOption
        {
            Id = "ogg",
            DisplayName = "OGG (.ogg)",
            Extension = "ogg",
            MuxerName = "ogg",
            SupportedKinds = [OutputKind.AudioOnly],
            VideoCodecIds = [],
            AudioCodecIds = ["vorbis", "opus", "flac"]
        },
        new ContainerOption
        {
            Id = "opus",
            DisplayName = "Opus (.opus)",
            Extension = "opus",
            MuxerName = "opus",
            SupportedKinds = [OutputKind.AudioOnly],
            VideoCodecIds = [],
            AudioCodecIds = ["opus"]
        }
    ];

    public static IReadOnlyList<VideoCodecOption> VideoCodecs { get; } =
    [
        new VideoCodecOption("h264", "H.264 / AVC", "libx264", "medium"),
        new VideoCodecOption("hevc", "H.265 / HEVC", "libx265", "medium"),
        new VideoCodecOption("vp9", "VP9", "libvpx-vp9", "medium")
    ];

    public static IReadOnlyList<AudioCodecOption> AudioCodecs { get; } =
    [
        new AudioCodecOption("aac", "AAC", "aac", true, 192),
        new AudioCodecOption("mp3", "MP3", "libmp3lame", true, 192),
        new AudioCodecOption("opus", "Opus", "libopus", true, 128),
        new AudioCodecOption("vorbis", "Vorbis", "libvorbis", true, 192),
        new AudioCodecOption("flac", "FLAC（无损）", "flac", false, 0),
        new AudioCodecOption("pcm", "PCM 16-bit（无损）", "pcm_s16le", false, 0)
    ];

    public static IReadOnlyList<VideoQualityOption> VideoQualities { get; } =
    [
        new VideoQualityOption("高质量（CRF 18）", 18),
        new VideoQualityOption("平衡（CRF 23）", 23),
        new VideoQualityOption("小体积（CRF 28）", 28)
    ];

    public static IReadOnlyList<AudioBitrateOption> AudioBitrates { get; } =
    [
        new AudioBitrateOption("128 kbps", 128),
        new AudioBitrateOption("192 kbps", 192),
        new AudioBitrateOption("256 kbps", 256),
        new AudioBitrateOption("320 kbps", 320)
    ];

    public static IReadOnlyList<ContainerOption> GetContainers(OutputKind kind)
    {
        return Containers.Where(container => container.SupportedKinds.Contains(kind)).ToList();
    }

    public static IReadOnlyList<VideoCodecOption> GetVideoCodecs(ContainerOption container)
    {
        return VideoCodecs
            .Where(codec => container.VideoCodecIds.Contains(codec.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    public static IReadOnlyList<AudioCodecOption> GetAudioCodecs(ContainerOption container)
    {
        return AudioCodecs
            .Where(codec => container.AudioCodecIds.Contains(codec.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    public static VideoCodecOption? FindVideoCodec(string id)
    {
        return VideoCodecs.FirstOrDefault(codec => string.Equals(codec.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static AudioCodecOption? FindAudioCodec(string id)
    {
        return AudioCodecs.FirstOrDefault(codec => string.Equals(codec.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
