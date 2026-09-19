namespace FfmpegGui.Core.Services;

public static class FFmpegLocator
{
    public static FFmpegPaths? TryLocate()
    {
        foreach (var directory in EnumerateCandidateDirectories())
        {
            var ffmpegPath = Path.Combine(directory, "ffmpeg.exe");
            var ffprobePath = Path.Combine(directory, "ffprobe.exe");

            if (File.Exists(ffmpegPath) && File.Exists(ffprobePath))
            {
                return new FFmpegPaths(ffmpegPath, ffprobePath, directory);
            }
        }

        return null;
    }

    public static FFmpegPaths Locate()
    {
        return TryLocate() ?? throw new FileNotFoundException(
            "未找到 ffmpeg.exe 和 ffprobe.exe。请将它们放入 tools/ffmpeg/ 目录，或与程序放在同一目录。");
    }

    private static IEnumerable<string> EnumerateCandidateDirectories()
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in EnumerateSelfAndParents(AppContext.BaseDirectory))
        {
            var toolsDirectory = Path.Combine(directory, "tools", "ffmpeg");
            if (visited.Add(toolsDirectory))
            {
                yield return toolsDirectory;
            }

            if (visited.Add(directory))
            {
                yield return directory;
            }
        }

        var configuredDirectory = Environment.GetEnvironmentVariable("FFMPEG_TOOLS_DIR");
        if (!string.IsNullOrWhiteSpace(configuredDirectory) && visited.Add(configuredDirectory))
        {
            yield return configuredDirectory;
        }

        var pathDirectories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var pathDirectory in pathDirectories)
        {
            var normalizedDirectory = pathDirectory.Trim('"');
            if (!string.IsNullOrWhiteSpace(normalizedDirectory) && visited.Add(normalizedDirectory))
            {
                yield return normalizedDirectory;
            }
        }
    }

    private static IEnumerable<string> EnumerateSelfAndParents(string path)
    {
        var current = new DirectoryInfo(path);

        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }
}
