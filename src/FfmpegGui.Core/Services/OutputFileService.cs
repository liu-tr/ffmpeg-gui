using System.Globalization;
using FfmpegGui.Core.Models;

namespace FfmpegGui.Core.Services;

public static class OutputFileService
{
    public static string CreateOutputPath(
        string inputPath,
        string outputDirectory,
        ContainerOption container,
        OutputKind outputKind)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory;
        }

        Directory.CreateDirectory(outputDirectory);

        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "output";
        }

        var suffix = outputKind switch
        {
            OutputKind.AudioOnly => "_audio",
            OutputKind.VideoOnly => "_video",
            _ => "_out"
        };

        var candidate = Path.Combine(outputDirectory, $"{baseName}{suffix}.{container.Extension}");
        var index = 1;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(outputDirectory, $"{baseName}{suffix}_{index}.{container.Extension}");
            index++;
        }

        return candidate;
    }
}
