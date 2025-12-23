using System;
using System.Threading.Tasks;
using YoutubeChannelFinder.Core;
using YoutubeChannelFinder.Infrastructure.Concurrency;

namespace YoutubeChannelFinder.Infrastructure.Decorators;

public sealed class ConcurrencyLimitedModule<TIn, TOut> : IPipelineModule<TIn, TOut>
{
    private readonly IPipelineModule<TIn, TOut> _inner;
    private readonly ModuleSemaphore _moduleSemaphore;
    private readonly int _maxConcurrency;
    private readonly string _key;

    public ConcurrencyLimitedModule(
        IPipelineModule<TIn, TOut> inner,
        ModuleSemaphore moduleSemaphore,
        int maxConcurrency,
        string? key = null)
    {
        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _moduleSemaphore = moduleSemaphore ?? throw new ArgumentNullException(nameof(moduleSemaphore));
        _maxConcurrency = maxConcurrency;
        _key = key ?? inner.Name;
    }

    public string Name => _inner.Name;

    public async Task<TOut> ExecuteAsync(
        TIn input,
        PipelineContext context)
    {
        using var permit = await _moduleSemaphore.AcquireAsync(
            key: _key,
            maxConcurrency: _maxConcurrency,
            cancellationToken: context.CancellationToken);

        return await _inner.ExecuteAsync(input, context);
    }
}
