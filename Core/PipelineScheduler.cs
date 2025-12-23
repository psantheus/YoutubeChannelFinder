using System;
using System.Threading;
using System.Threading.Tasks;

using YoutubeChannelFinder.Infrastructure.Concurrency;

namespace YoutubeChannelFinder.Core;

public sealed class PipelineScheduler
{
    private readonly PipelineOrchestrator _orchestrator;
    private readonly GlobalSemaphore _globalSemaphore;

    public PipelineScheduler(
        PipelineOrchestrator orchestrator,
        GlobalSemaphore globalSemaphore
    )
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _globalSemaphore = globalSemaphore ?? throw new ArgumentNullException(nameof(globalSemaphore));
    }

    public async Task<object> RunAsync(
        object input,
        PipelineContext context
    )
    {
        using var permit = await _globalSemaphore.AcquireAsync(context.CancellationToken);
        return await _orchestrator.RunAsync(input, context);
    }
}