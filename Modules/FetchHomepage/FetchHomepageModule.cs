using System;
using System.Net.Http;
using System.Threading.Tasks;
using YoutubeChannelFinder.Core;
using YoutubeChannelFinder.Infrastructure.Http;

namespace YoutubeChannelFinder.Modules.FetchHomepage;

public sealed class FetchHomepageModule
    : IPipelineModule<string, FetchHomepageResult>
{
    public string Name => "FetchHomepage";

    public async Task<FetchHomepageResult> ExecuteAsync(
        string input,
        PipelineContext context)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return FetchHomepageResult.Fail(
                url: input,
                statusCode: null,
                error: "Input domain is empty");
        }

        string url;
        try
        {
            url = NormalizeUrl(input);
        }
        catch (Exception ex)
        {
            return FetchHomepageResult.Fail(
                url: input,
                statusCode: null,
                error: ex.Message);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await HttpClientProvider.Client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                context.CancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return FetchHomepageResult.Fail(
                    url,
                    (int)response.StatusCode,
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var content = await response.Content.ReadAsStringAsync(
                context.CancellationToken);

            return FetchHomepageResult.Ok(
                url: url,
                html: content,
                statusCode: (int)response.StatusCode,
                contentType: response.Content.Headers.ContentType?.ToString() ?? "",
                contentLength: content.Length);
        }
        catch (OperationCanceledException)
        {
            return FetchHomepageResult.Fail(
                url,
                null,
                "Request cancelled");
        }
        catch (Exception ex)
        {
            return FetchHomepageResult.Fail(
                url,
                null,
                ex.Message);
        }
    }

    private static string NormalizeUrl(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        if (Uri.TryCreate("https://" + input, UriKind.Absolute, out var https))
            return https.ToString();

        throw new InvalidOperationException($"Invalid URL: {input}");
    }
}
