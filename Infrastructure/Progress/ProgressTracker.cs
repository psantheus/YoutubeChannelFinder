using System;
using System.Diagnostics;
using System.Threading;

namespace YoutubeChannelFinder.Infrastructure.Progress;

public sealed class ProgressTracker
{
    private int _completed;
    private readonly int _total;
    private readonly Stopwatch _stopwatch;

    public ProgressTracker(int total)
    {
        if (total <= 0)
            throw new ArgumentOutOfRangeException(nameof(total));

        _total = total;
        _stopwatch = Stopwatch.StartNew();
    }

    public DateTime StartTime { get; } = DateTime.UtcNow;

    public int Completed => Volatile.Read(ref _completed);
    public int Total => _total;

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public double AveragePerItemSeconds =>
        Completed == 0 ? 0 : Elapsed.TotalSeconds / Completed;

    public DateTime? EstimatedFinishTime
    {
        get
        {
            if (Completed == 0)
                return null;

            var remaining = Total - Completed;
            var remainingSeconds = remaining * AveragePerItemSeconds;
            return DateTime.UtcNow.AddSeconds(remainingSeconds);
        }
    }

    public void MarkCompleted()
    {
        Interlocked.Increment(ref _completed);
    }
}
