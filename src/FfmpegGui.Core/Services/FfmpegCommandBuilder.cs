using System.Globalization;
using FfmpegGui.Core.Models;

namespace FfmpegGui.Core.Services;

public static class FfmpegCommandBuilder
{
    public static IReadOnlyList<string> Build(OutputSettings settings)
    {
        var arguments = new List<string>
        {
            "-hide_banner",
            "-nostdin",
            "-y",
            "-progress",
            "pipe:1",
            "-nostats",
            "-i",
            settings.InputPath
        };

        switch (settings.OutputKind)
        {
            case OutputKind.AudioOnly:
                arguments.Add("-vn");
                arguments.Add("-map");
                arguments.Add($"0:a:{settings.AudioStreamOrdinal}?");
                break;
            case OutputKind.VideoOnly:
                arguments.Add("-an");
                arguments.Add("-map");
                arguments.Add($"0:v:{settings.VideoStreamOrdinal}?");
                break;
            default:
                arguments.Add("-map");
                arguments.Add($"0:v:{settings.VideoStreamOrdinal}?");
                arguments.Add("-map");
                arguments.Add($"0:a:{settings.AudioStreamOrdinal}?");
                break;
        }

        if (settings.OutputKind != OutputKind.AudioOnly)
        {
            AddVideoArguments(arguments, settings);
        }

        if (settings.OutputKind != OutputKind.VideoOnly)
        {
            AddAudioArguments(arguments, settings);
        }

        if (settings.Container.MuxerName is "mp4" or "mov" or "ipod")
        {
            arguments.Add("-movflags");
            arguments.Add("+faststart");
        }

        arguments.Add("-f");
        arguments.Add(settings.Container.MuxerName);
        arguments.Add(settings.OutputPath);

        return arguments;
    }

    private static void AddVideoArguments(List<string> arguments, OutputSettings settings)
    {
        var codec = settings.VideoCodec ?? throw new InvalidOperationException("未选择视频编码器。");
        var quality = settings.VideoQuality?.Crf ?? 23;
        var crf = quality.ToString(CultureInfo.InvariantCulture);

        switch (codec.Id.ToLowerInvariant())
        {
            case "h264":
                arguments.AddRange(["-c:v", "libx264", "-preset", "medium", "-crf", crf, "-pix_fmt", "yuv420p"]);
                break;
            case "hevc":
                arguments.AddRange(["-c:v", "libx265", "-preset", "medium", "-crf", crf, "-pix_fmt", "yuv420p"]);
                if (settings.Container.MuxerName is "mp4" or "mov")
                {
                    arguments.AddRange(["-tag:v", "hvc1"]);
                }
                break;
            case "vp9":
                arguments.AddRange(["-c:v", "libvpx-vp9", "-crf", crf, "-b:v", "0", "-pix_fmt", "yuv420p"]);
                break;
            default:
                throw new InvalidOperationException($"不支持的视频编码器：{codec.DisplayName}");
        }
    }

    private static void AddAudioArguments(List<string> arguments, OutputSettings settings)
    {
        var codec = settings.AudioCodec ?? throw new InvalidOperationException("未选择音频编码器。");
        var bitrate = settings.AudioBitrate?.BitrateKbps ?? codec.DefaultBitrateKbps;
        var bitrateText = bitrate.ToString(CultureInfo.InvariantCulture) + "k";

        switch (codec.Id.ToLowerInvariant())
        {
            case "aac":
                arguments.AddRange(["-c:a", "aac", "-b:a", bitrateText]);
                break;
            case "mp3":
                arguments.AddRange(["-c:a", "libmp3lame", "-b:a", bitrateText]);
                break;
            case "opus":
                arguments.AddRange(["-c:a", "libopus", "-b:a", bitrateText]);
                break;
            case "vorbis":
                arguments.AddRange(["-c:a", "libvorbis", "-b:a", bitrateText]);
                break;
            case "flac":
                arguments.AddRange(["-c:a", "flac"]);
                break;
            case "pcm":
                arguments.AddRange(["-c:a", "pcm_s16le"]);
                break;
            default:
                throw new InvalidOperationException($"不支持的音频编码器：{codec.DisplayName}");
        }
    }
}

