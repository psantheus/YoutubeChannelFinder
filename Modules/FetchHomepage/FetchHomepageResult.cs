using YoutubeChannelFinder.Core;

namespace YoutubeChannelFinder.Modules.FetchHomepage;

public sealed class FetchHomepageResult : IFailableResult
{
    public bool Success { get; init; }

    public string Url { get; init; } = string.Empty;

    public int? StatusCode { get; init; }

    public string? Html { get; init; }

    public int ContentLength { get; init; }

    public string ContentType { get; init; } = string.Empty;

    public string? Error { get; init; }

    public static FetchHomepageResult Ok(
        string url,
        string html,
        int statusCode,
        string contentType,
        int contentLength)
        => new()
        {
            Success = true,
            Url = url,
            Html = html,
            StatusCode = statusCode,
            ContentType = contentType,
            ContentLength = contentLength
        };

    public static FetchHomepageResult Fail(
        string url,
        int? statusCode,
        string error)
        => new()
        {
            Success = false,
            Url = url,
            StatusCode = statusCode,
            Error = error
        };
}
