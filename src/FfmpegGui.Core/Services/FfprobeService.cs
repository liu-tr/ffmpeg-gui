using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using FfmpegGui.Core.Models;

namespace FfmpegGui.Core.Services;

public sealed class FfprobeService
{
    private readonly string _ffprobePath;

    public FfprobeService(string ffprobePath)
    {
        if (string.IsNullOrWhiteSpace(ffprobePath))
        {
            throw new ArgumentException("ffprobe 路径不能为空。", nameof(ffprobePath));
        }

        _ffprobePath = ffprobePath;
    }

    public FfprobeService(FFmpegPaths paths) : this(paths.FFprobePath)
    {
    }

    public async Task<MediaInfo> ProbeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("媒体文件路径不能为空。", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("媒体文件不存在。", filePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-print_format");
        startInfo.ArgumentList.Add("json");
        startInfo.ArgumentList.Add("-show_format");
        startInfo.ArgumentList.Add("-show_streams");
        startInfo.ArgumentList.Add(filePath);

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("无法启动 ffprobe 进程。");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);

            try
            {
                await Task.WhenAll(standardOutputTask, standardErrorTask).ConfigureAwait(false);
            }
            catch
            {
                // 进程结束后读取任务可能被中断，这里只需要确保异常不向上传播。
            }

            throw;
        }

        var json = await standardOutputTask.ConfigureAwait(false);
        var error = await standardErrorTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new FfprobeException(
                $"ffprobe 执行失败（退出码 {process.ExitCode}）：{error.Trim()}",
                process.ExitCode);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return ParseMediaInfo(filePath, document.RootElement);
        }
        catch (JsonException exception)
        {
            throw new FfprobeException("ffprobe 返回的 JSON 无法解析。", exception);
        }
    }

    private static MediaInfo ParseMediaInfo(string filePath, JsonElement root)
    {
        var format = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("format", out var formatElement)
            ? formatElement
            : default;

        var streams = new List<MediaStreamInfo>();

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("streams", out var streamArray) &&
            streamArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var stream in streamArray.EnumerateArray())
            {
                streams.Add(ParseStream(stream));
            }
        }

        var duration = GetDouble(format, "duration");

        return new MediaInfo
        {
            FilePath = filePath,
            FormatName = GetString(format, "format_name") ?? string.Empty,
            FormatLongName = GetString(format, "format_long_name") ?? string.Empty,
            Duration = duration is > 0 ? TimeSpan.FromSeconds(duration.Value) : null,
            SizeBytes = GetInt64(format, "size"),
            BitRate = GetInt64(format, "bit_rate"),
            Streams = streams
        };
    }

    private static MediaStreamInfo ParseStream(JsonElement stream)
    {
        var tags = stream.ValueKind == JsonValueKind.Object && stream.TryGetProperty("tags", out var tagsElement)
            ? tagsElement
            : default;

        var disposition = stream.ValueKind == JsonValueKind.Object && stream.TryGetProperty("disposition", out var dispositionElement)
            ? dispositionElement
            : default;

        return new MediaStreamInfo
        {
            Index = GetInt32(stream, "index") ?? 0,
            StreamType = ParseStreamType(GetString(stream, "codec_type")),
            CodecName = GetString(stream, "codec_name") ?? string.Empty,
            CodecLongName = GetString(stream, "codec_long_name") ?? string.Empty,
            Profile = GetString(stream, "profile"),
            Width = GetInt32(stream, "width"),
            Height = GetInt32(stream, "height"),
            PixelFormat = GetString(stream, "pix_fmt"),
            FrameRate = ParseFrameRate(GetString(stream, "avg_frame_rate") ?? GetString(stream, "r_frame_rate")),
            BitRate = GetInt64(stream, "bit_rate"),
            SampleRate = GetInt32(stream, "sample_rate"),
            Channels = GetInt32(stream, "channels"),
            ChannelLayout = GetString(stream, "channel_layout"),
            DurationSeconds = GetDouble(stream, "duration"),
            Language = GetTag(tags, "language"),
            Title = GetTag(tags, "title"),
            IsDefault = GetInt32(disposition, "default") == 1,
            IsForced = GetInt32(disposition, "forced") == 1
        };
    }

    private static MediaStreamType ParseStreamType(string? streamType)
    {
        return streamType?.ToLowerInvariant() switch
        {
            "video" => MediaStreamType.Video,
            "audio" => MediaStreamType.Audio,
            "subtitle" => MediaStreamType.Subtitle,
            "data" => MediaStreamType.Data,
            "attachment" => MediaStreamType.Attachment,
            _ => MediaStreamType.Unknown
        };
    }

    private static double? ParseFrameRate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Split('/', 2);
        if (parts.Length == 2 &&
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator) &&
            denominator != 0)
        {
            return numerator / denominator;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var frameRate)
            ? frameRate
            : null;
    }

    private static string? GetTag(JsonElement tags, string tagName)
    {
        return tags.ValueKind == JsonValueKind.Object &&
               tags.TryGetProperty(tagName, out var tagValue) &&
               tagValue.ValueKind == JsonValueKind.String
            ? tagValue.GetString()
            : null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            _ => null
        };
    }

    private static int? GetInt32(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        return property.ValueKind == JsonValueKind.String &&
               int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static long? GetInt64(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var number))
        {
            return number;
        }

        return property.ValueKind == JsonValueKind.String &&
               long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var number))
        {
            return number;
        }

        return property.ValueKind == JsonValueKind.String &&
               double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // 取消或进程已经结束时忽略清理异常。
        }
    }
}


