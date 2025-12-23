using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeChannelFinder.Infrastructure.Logging;

public sealed class LogBuffer
{
    private readonly ConcurrentQueue<string> _lines = new();
    private readonly int _maxLines;

    public LogBuffer(int maxLines = 500)
    {
        _maxLines = maxLines;
    }

    public void Add(string line)
    {
        _lines.Enqueue(line);

        while (_lines.Count > _maxLines && _lines.TryDequeue(out _)) { }
    }

    public IReadOnlyList<string> Snapshot(int lastN = 200)
    {
        return _lines.Reverse().Take(lastN).Reverse().ToList();
    }
}
