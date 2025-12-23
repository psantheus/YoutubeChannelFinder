using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YoutubeChannelFinder.Infrastructure.Persistence;

namespace YoutubeChannelFinder.Core;

public sealed class PipelineOrchestrator
{
    private readonly IReadOnlyList<IPipelineStep> _steps;
    private readonly FileAuditWriter _audit;

    public PipelineOrchestrator(
        IReadOnlyList<IPipelineStep> steps,
        FileAuditWriter audit)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<TOutput> ExecuteAsync<TInput, TOutput>(
        TInput initialInput,
        PipelineContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        object? current = initialInput;
        var succeededModules = new List<string>();

        try
        {
            foreach (var step in _steps)
            {
                // Persist module input
                _audit.WriteModuleInput(
                    context.InputId,
                    step.Name,
                    current!);

                try
                {
                    current = await step.ExecuteAsync(current!, context);

                    // Persist successful output
                    _audit.WriteModuleSuccess(
                        context.InputId,
                        step.Name,
                        current!);

                    succeededModules.Add(step.Name);
                }
                catch (Exception ex)
                {
                    // Persist module failure
                    _audit.WriteModuleFailure(
                        context.InputId,
                        step.Name,
                        ex);

                    // Persist input-level summary
                    _audit.WriteInputSummary(
                        context.InputId,
                        succeededModules,
                        failedModule: step.Name,
                        error: ex);

                    throw;
                }
            }

            // Persist success summary
            _audit.WriteInputSummary(
                context.InputId,
                succeededModules,
                failedModule: null,
                error: null);

            return (TOutput)current!;
        }
        catch
        {
            // Do not swallow exceptions
            throw;
        }
    }
}
