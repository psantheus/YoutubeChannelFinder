using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YoutubeChannelFinder.Core;

public sealed class PipelineOrchestrator
{
    private readonly IReadOnlyList<IPipelineStep> _steps;

    public PipelineOrchestrator(IReadOnlyList<IPipelineStep> steps)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    public async Task<object> RunAsync(
        object initialInput,
        PipelineContext context
    )
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        object current = initialInput;

        foreach (var step in _steps)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            current = await step.ExecuteAsync(current, context);
        }

        return current;
    }
}
