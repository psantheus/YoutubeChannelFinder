using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeChannelFinder.Infrastructure.Concurrency;

public sealed class ModuleSemaphore
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<IDisposable> AcquireAsync(
        string key,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key must be provided.", nameof(key));

        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

        var semaphore = _semaphores.GetOrAdd(
            key,
            _ => new SemaphoreSlim(maxConcurrency, maxConcurrency)
        );

        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
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
