using System;
using System.Collections.Generic;

namespace YoutubeChannelFinder.Core;

public static class PipelineValidator
{
    public static void Validate(
        IReadOnlyList<IPipelineStep> steps,
        Type initialInputType
    )
    {
        if (steps == null || steps.Count == 0)
        {
            throw new InvalidOperationException("Pipeline must contain at least one step.");
        }

        var currentType = initialInputType;

        foreach (var step in steps)
        {
            if (step.InputType != currentType)
            {
                throw new InvalidOperationException(
                    $"Invalid pipeline chain at step '{step.Name}'. " +
                    $"Expected input type {currentType.Name}, " +
                    $"but step requires {step.InputType.Name}."
                );
            }

            currentType = step.OutputType;
        }
    }
}
