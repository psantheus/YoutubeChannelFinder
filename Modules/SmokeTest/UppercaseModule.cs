using System.Threading.Tasks;
using YoutubeChannelFinder.Core;

namespace YoutubeChannelFinder;

public sealed class UppercaseModule : IPipelineModule<string, string>
{
    public string Name => "Uppercase";

    public async Task<string> ExecuteAsync(
        string input,
        PipelineContext context
    )
    {
        await Task.Delay(500, context.CancellationToken); // simulate async work
        return input.ToUpperInvariant();
    }
}
