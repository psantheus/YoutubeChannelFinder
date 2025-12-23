using System;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeChannelFinder.Infrastructure.Concurrency;

public sealed class GlobalSemaphore
{
    private readonly SemaphoreSlim _semaphore;

    public GlobalSemaphore(int maxConcurrency)
    {
        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Releaser(_semaphore);
    }

    private sealed class Releaser : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            _semaphore?.Release();
            _semaphore = null;
        }
    }
}
