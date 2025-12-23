namespace YoutubeChannelFinder.Core;

public interface IFailableResult
{
    bool Success { get; }
    string? Error { get; }
}
