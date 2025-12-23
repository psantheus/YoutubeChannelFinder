using System;

namespace YoutubeChannelFinder.Infrastructure.Logging;

public sealed class ConsolePipelineLogger : IPipelineLogger
{
    private readonly LogBuffer _buffer;

    public ConsolePipelineLogger(LogBuffer buffer)
    {
        _buffer = buffer;
    }

    public void Info(string message, Guid correlationId)
        => Write("INFO", message, correlationId);

    public void Warn(string message, Guid correlationId)
        => Write("WARN", message, correlationId);

    public void Error(string message, Guid correlationId, Exception? exception = null)
    {
        var full = exception == null
            ? message
            : $"{message} | {exception.GetType().Name}: {exception.Message}";

        Write("ERROR", full, correlationId);
    }

    private void Write(string level, string message, Guid correlationId)
    {
        _buffer.Add(
            $"[{level}] [{correlationId}] {message}"
        );
    }
}
