using System.Threading.Tasks;

namespace YoutubeChannelFinder.Core;

public interface IPipelineModule<TIn, TOut>
{
    string Name { get; }

    Task<TOut> ExecuteAsync(
        TIn input,
        PipelineContext context
    );
}
