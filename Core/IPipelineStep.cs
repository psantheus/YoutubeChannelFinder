using System;
using System.Threading.Tasks;

namespace YoutubeChannelFinder.Core;

public interface IPipelineStep
{
    string Name { get; }

    Type InputType { get; }
    Type OutputType { get; }

    Task<object> ExecuteAsync(
        object input,
        PipelineContext context
    );
}
