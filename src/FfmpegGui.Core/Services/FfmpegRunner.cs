using System.Diagnostics;
using System.Globalization;
using System.Text;
using FfmpegGui.Core.Models;

namespace FfmpegGui.Core.Services;

public sealed class FfmpegRunner
{
    private readonly string _ffmpegPath;

    public FfmpegRunner(string ffmpegPath)
    {
        if (string.IsNullOrWhiteSpace(ffmpegPath))
        {
            throw new ArgumentException("ffmpeg 路径不能为空。", nameof(ffmpegPath));
        }

        _ffmpegPath = ffmpegPath;
    }

    public FfmpegRunner(FFmpegPaths paths) : this(paths.FFmpegPath)
    {
    }

    public async Task RunAsync(
        IReadOnlyList<string> arguments,
        TimeSpan? totalDuration,
        IProgress<FfmpegProgress>? progress = null,
        IProgress<string>? logProgress = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("无法启动 ffmpeg 进程。");
        }

        var standardErrorOutput = new StringBuilder();
        var standardErrorTask = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var line = await process.StandardError.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (line is null)
                    {
                        break;
                    }

                    standardErrorOutput.AppendLine(line);
                    logProgress?.Report(line);
                }
            }
            catch (OperationCanceledException)
            {
                // 取消时进程会被终止，这里只需要结束日志读取。
            }
        });

        TimeSpan? processedDuration = null;
        double? speed = null;
        long? totalSizeBytes = null;

        try
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                HandleProgressLine(line, totalDuration, ref processedDuration, ref speed, ref totalSizeBytes, progress);
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);

            try
            {
                await standardErrorTask.ConfigureAwait(false);
            }
            catch
            {
                // 进程被终止后读取错误输出可能中断，这里忽略清理异常。
            }

            throw;
        }

        await standardErrorTask.ConfigureAwait(false);
        var errorOutput = standardErrorOutput.ToString().Trim();

        if (process.ExitCode != 0)
        {
            throw new FfmpegException(
                $"ffmpeg 执行失败（退出码 {process.ExitCode}）。",
                process.ExitCode,
                errorOutput);
        }

        progress?.Report(new FfmpegProgress(
            processedDuration ?? totalDuration,
            100,
            speed,
            totalSizeBytes,
            "end"));
    }

    private static void HandleProgressLine(
        string line,
        TimeSpan? totalDuration,
        ref TimeSpan? processedDuration,
        ref double? speed,
        ref long? totalSizeBytes,
        IProgress<FfmpegProgress>? progress)
    {
        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return;
        }

        var key = line[..separatorIndex];
        var value = line[(separatorIndex + 1)..];

        switch (key)
        {
            case "out_time_us":
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var microSeconds))
                {
                    processedDuration = TimeSpan.FromTicks(microSeconds * 10);
                }
                break;

            case "speed":
                var speedText = value.TrimEnd('x').Trim();
                if (double.TryParse(speedText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedSpeed))
                {
                    speed = parsedSpeed;
                }
                break;

            case "total_size":
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSize))
                {
                    totalSizeBytes = parsedSize;
                }
                break;

            case "progress":
                progress?.Report(new FfmpegProgress(
                    processedDuration,
                    CalculatePercentage(processedDuration, totalDuration),
                    speed,
                    totalSizeBytes,
                    value));
                break;
        }
    }

    private static double? CalculatePercentage(TimeSpan? processedDuration, TimeSpan? totalDuration)
    {
        if (processedDuration is null ||
            totalDuration is null ||
            totalDuration.Value.TotalMilliseconds <= 0)
        {
            return null;
        }

        return Math.Clamp(
            processedDuration.Value.TotalMilliseconds / totalDuration.Value.TotalMilliseconds * 100,
            0,
            100);
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
            // 进程已经退出时忽略清理异常。
        }
    }
}
