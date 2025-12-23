using System;
using System.Net.Http;
using System.Threading.Tasks;
using YoutubeChannelFinder.Core;

namespace YoutubeChannelFinder.Modules.FetchHomepage;

public sealed class FetchHomepageModule : IPipelineModule<string, string>
{
    private readonly HttpClient _httpClient;

    public FetchHomepageModule(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string Name => "FetchHomepage";

    public async Task<string> ExecuteAsync(
        string input,
        PipelineContext context)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty", nameof(input));

        var url = NormalizeUrl(input);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (compatible; YoutubeChannelFinder/1.0)");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            context.CancellationToken);

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(
            context.CancellationToken);

        // Attempt-scoped storage (safe with Option A)
        context.Bag["homepage_url"] = url.ToString();
        context.Bag["homepage_html"] = html;

        return html;
    }

    private static Uri NormalizeUrl(string input)
    {
        // Absolute URL already
        if (Uri.TryCreate(input, UriKind.Absolute, out var absolute))
            return absolute;

        // Try https:// + input
        if (Uri.TryCreate($"https://{input}", UriKind.Absolute, out var https))
            return https;

        throw new InvalidOperationException($"Invalid URL input: {input}");
    }
}
