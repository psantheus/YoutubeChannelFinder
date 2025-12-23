using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace YoutubeChannelFinder.Infrastructure.UI;

public sealed class ActiveJobTracker
{
    private sealed record Job(
        string Input,
        string Stage,
        Stopwatch Stopwatch
    );

    private readonly ConcurrentDictionary<Guid, Job> _jobs = new();

    public void Start(Guid correlationId, string input)
    {
        _jobs[correlationId] = new Job(
            input,
            Stage: "Starting",
            Stopwatch.StartNew()
        );
    }

    public void UpdateStage(Guid correlationId, string stage)
    {
        if (_jobs.TryGetValue(correlationId, out var job))
        {
            _jobs[correlationId] = job with { Stage = stage };
        }
    }

    public void Complete(Guid correlationId)
    {
        _jobs.TryRemove(correlationId, out _);
    }

    public IReadOnlyCollection<(string Input, string Stage, TimeSpan Elapsed)> Snapshot()
    {
        return _jobs.Values.Select(j =>
            (j.Input, j.Stage, j.Stopwatch.Elapsed)
        ).ToList();
    }

    public int ActiveCount => _jobs.Count;
}
