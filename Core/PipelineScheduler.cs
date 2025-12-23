using System;
using System.Threading.Tasks;
using YoutubeChannelFinder.Infrastructure.Concurrency;

namespace YoutubeChannelFinder.Core;

public sealed class PipelineScheduler
{
    private readonly PipelineOrchestrator _orchestrator;
    private readonly GlobalSemaphore _globalSemaphore;

    public PipelineScheduler(
        PipelineOrchestrator orchestrator,
        GlobalSemaphore globalSemaphore)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _globalSemaphore = globalSemaphore ?? throw new ArgumentNullException(nameof(globalSemaphore));
    }

    public async Task<TOutput> RunAsync<TInput, TOutput>(
        TInput input,
        PipelineContext context)
    {
        // Acquire global concurrency slot
        using (await _globalSemaphore.AcquireAsync(context.CancellationToken))
        {
            // Execute pipeline
            return await _orchestrator.ExecuteAsync<TInput, TOutput>(
                input,
                context);
        }
    }
}
