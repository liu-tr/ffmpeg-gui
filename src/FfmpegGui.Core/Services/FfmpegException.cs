namespace FfmpegGui.Core.Services;

public sealed class FfmpegException : Exception
{
    public int ExitCode { get; }
    public string ErrorOutput { get; }

    public FfmpegException(string message, int exitCode, string errorOutput) : base(message)
    {
        ExitCode = exitCode;
        ErrorOutput = errorOutput;
    }
}
