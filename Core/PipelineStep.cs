using System;
using System.Threading.Tasks;

namespace YoutubeChannelFinder.Core;

public sealed class PipelineStep<TIn, TOut> : IPipelineStep
{
    private readonly IPipelineModule<TIn, TOut> _module;

    public PipelineStep(IPipelineModule<TIn, TOut> module)
    {
        _module = module ?? throw new ArgumentNullException(nameof(module));
    }

    public string Name => _module.Name;

    public Type InputType => typeof(TIn);
    public Type OutputType => typeof(TOut);

    public async Task<object> ExecuteAsync(
        object input,
        PipelineContext context
    )
    {
        if (input is not TIn typedInput)
        {
            throw new InvalidCastException(
                $"Step '{Name}' expected input of type {typeof(TIn).Name} " +
                $"but received {input.GetType().Name}"
            );
        }

        return await _module.ExecuteAsync(typedInput, context);
    }
}
