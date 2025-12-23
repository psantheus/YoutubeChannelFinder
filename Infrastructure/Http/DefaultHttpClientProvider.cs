using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace YoutubeChannelFinder.Infrastructure.Http;

public sealed class DefaultHttpClientProvider : IHttpClientProvider
{
    private readonly ConcurrentDictionary<string, HttpClient> _clients =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly TimeSpan _defaultTimeout;

    public DefaultHttpClientProvider(TimeSpan? defaultTimeout = null)
    {
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
    }

    public HttpClient GetClient(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Client name must be provided.", nameof(name));

        return _clients.GetOrAdd(name, CreateClient);
    }

    private HttpClient CreateClient(string name)
    {
        var client = new HttpClient
        {
            Timeout = _defaultTimeout
        };

        // Future:
        // - Default headers
        // - User-Agent
        // - Proxy
        // - DNS refresh
        // - HttpMessageHandler chain

        return client;
    }
}
