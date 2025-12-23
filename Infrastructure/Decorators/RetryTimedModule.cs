using System;
using System.Threading;
using System.Threading.Tasks;
using YoutubeChannelFinder.Core;

namespace YoutubeChannelFinder.Infrastructure.Decorators;

public sealed class RetryTimedModule<TIn, TOut> : IPipelineModule<TIn, TOut>
{
    private readonly IPipelineModule<TIn, TOut> _inner;
    private readonly int _maxRetries;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _delayBetweenRetries;

    public RetryTimedModule(
        IPipelineModule<TIn, TOut> inner,
        int maxRetries,
        TimeSpan timeout,
        TimeSpan? delayBetweenRetries = null)
    {
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries));

        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _maxRetries = maxRetries;
        _timeout = timeout;
        _delayBetweenRetries = delayBetweenRetries ?? TimeSpan.Zero;
    }

    public string Name => _inner.Name;

    public async Task<TOut> ExecuteAsync(
        TIn input,
        PipelineContext context)
    {
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            using var attemptCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    context.CancellationToken);

            attemptCts.CancelAfter(_timeout);

            var attemptContext = context.CloneForAttempt(attemptCts.Token);

            try
            {
                return await _inner.ExecuteAsync(input, attemptContext);
            }
            catch (OperationCanceledException)
                when (!context.CancellationToken.IsCancellationRequested &&
                      attempt < _maxRetries)
            {
                // Timeout on this attempt – retry
            }
            catch (Exception)
                when (attempt < _maxRetries)
            {
                // Transient failure – retry
            }

            if (_delayBetweenRetries > TimeSpan.Zero)
            {
                await Task.Delay(_delayBetweenRetries, context.CancellationToken);
            }
        }

        // Should never be reached, but keeps compiler happy
        throw new InvalidOperationException("Retry loop exited unexpectedly.");
    }
}
