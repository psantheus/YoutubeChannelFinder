using System;

namespace YoutubeChannelFinder.Infrastructure.Logging;

public interface IPipelineLogger
{
    void Info(string message, Guid correlationId);
    void Warn(string message, Guid correlationId);
    void Error(string message, Guid correlationId, Exception? exception = null);
}
