namespace FfmpegGui.Core.Services;

public sealed class FfprobeException : Exception
{
    public int? ExitCode { get; }

    public FfprobeException(string message, int? exitCode = null) : base(message)
    {
        ExitCode = exitCode;
    }

    public FfprobeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
