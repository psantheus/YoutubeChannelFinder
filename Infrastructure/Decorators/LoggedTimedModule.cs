using YoutubeChannelFinder.Core;
using YoutubeChannelFinder.Infrastructure.Logging;
using YoutubeChannelFinder.Infrastructure.UI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace YoutubeChannelFinder.Infrastructure.Decorators;

public sealed class LoggedTimedModule<TIn, TOut> : IPipelineModule<TIn, TOut>
{
    private readonly IPipelineModule<TIn, TOut> _inner;
    private readonly IPipelineLogger _logger;
    private readonly ActiveJobTracker _activeJobs;

    public LoggedTimedModule(
        IPipelineModule<TIn, TOut> inner,
        IPipelineLogger logger,
        ActiveJobTracker activeJobs)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activeJobs = activeJobs ?? throw new ArgumentNullException(nameof(activeJobs));
    }

    public string Name => _inner.Name;

    public async Task<TOut> ExecuteAsync(
        TIn input,
        PipelineContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        _activeJobs.UpdateStage(context.CorrelationId, Name);
        _logger.Info($"{context.InputId} | {Name} | started", context.CorrelationId);

        try
        {
            var result = await _inner.ExecuteAsync(input, context);

            stopwatch.Stop();
            _logger.Info($"{context.InputId} | {Name} | completed in {stopwatch.ElapsedMilliseconds} ms", context.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error($"{context.InputId} | {Name} | failed after {stopwatch.ElapsedMilliseconds} ms", context.CorrelationId, ex);

            throw;
        }
    }
}
