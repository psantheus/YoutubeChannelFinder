using System;
using System.Net.Http;

namespace YoutubeChannelFinder.Infrastructure.Http;

public static class HttpClientProvider
{
    public static readonly HttpClient Client = new HttpClient
    {
        Timeout = Timeout.InfiniteTimeSpan
    };
}
