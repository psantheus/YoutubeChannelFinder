using System.Net.Http;

namespace YoutubeChannelFinder.Infrastructure.Http;

public interface IHttpClientProvider
{
    HttpClient GetClient(string name);
}
