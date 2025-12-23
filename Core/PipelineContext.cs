using System;
using System.Collections.Generic;
using System.Threading;

namespace YoutubeChannelFinder.Core;

public sealed class PipelineContext
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    // Logical identifier for the input (e.g. domain)
    public string InputId { get; init; } = string.Empty;

    // ATTEMPT-SCOPED state bag
    // Fresh for each retry attempt
    public IDictionary<string, object> Bag { get; init; } =
        new Dictionary<string, object>();

    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Creates a new context for a retry attempt.
    /// Bag is intentionally reset.
    /// CorrelationId and InputId are preserved.
    /// </summary>
    public PipelineContext CloneForAttempt(CancellationToken cancellationToken)
    {
        return new PipelineContext
        {
            CorrelationId = CorrelationId,
            InputId = InputId,
            CancellationToken = cancellationToken,
            Bag = new Dictionary<string, object>()
        };
    }
}
