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

                    // 🔴 NEW: detect domain failure (non-exception)
                    if (current is IFailableResult failable && !failable.Success)
                    {
                        // Log module failure
                        _audit.WriteModuleFailure(
                            context.InputId,
                            step.Name,
                            new Exception(failable.Error ?? "Module reported failure"));

                        // Log input-level failure
                        _audit.WriteInputSummary(
                            context.InputId,
                            succeededModules,
                            failedModule: step.Name,
                            error: new Exception(failable.Error ?? "Module reported failure"));

                        // Stop pipeline for this input
                        return default!;
                    }

                    // Persist successful output
                    _audit.WriteModuleSuccess(
                        context.InputId,
                        step.Name,
                        current!);

                    succeededModules.Add(step.Name);
                }
                catch (Exception ex)
                {
                    // Persist module failure (exceptional)
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

            // Persist success summary (only reached if all modules succeeded)
            _audit.WriteInputSummary(
                context.InputId,
                succeededModules,
                failedModule: null,
                error: null);

            return (TOutput)current!;
        }
        catch
        {
            throw;
        }
    }
}
