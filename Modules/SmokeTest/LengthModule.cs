using System.Threading.Tasks;
using YoutubeChannelFinder.Core;

namespace YoutubeChannelFinder;

public sealed class LengthModule : IPipelineModule<string, int>
{
    public string Name => "Length";

    public async Task<int> ExecuteAsync(
        string input,
        PipelineContext context
    )
    {
        await Task.Delay(300, context.CancellationToken); // simulate async work
        return input.Length;
    }
}
