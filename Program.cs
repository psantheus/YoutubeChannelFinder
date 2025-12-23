using Spectre.Console;
using YoutubeChannelFinder.Core;
using YoutubeChannelFinder.Infrastructure.Concurrency;
using YoutubeChannelFinder.Infrastructure.Decorators;
using YoutubeChannelFinder.Infrastructure.Logging;
using YoutubeChannelFinder.Infrastructure.Progress;
using YoutubeChannelFinder.Infrastructure.UI;

namespace YoutubeChannelFinder;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Pipeline Engine – Smoke Test");
        Console.WriteLine("--------------------------------");

        var logBuffer = new LogBuffer();
        var logger = new ConsolePipelineLogger(logBuffer);
        var activeJobs = new ActiveJobTracker();
        var moduleSemaphore = new ModuleSemaphore();

        var uppercase =
            new LoggedTimedModule<string, string>(
                new RetryTimedModule<string, string>(
                    new ConcurrencyLimitedModule<string, string>(
                        new UppercaseModule(),
                        moduleSemaphore,
                        maxConcurrency: 2),
                    3,
                    TimeSpan.FromMilliseconds(30000)),
                logger,
                activeJobs);

        var length =
            new LoggedTimedModule<string, int>(
                new RetryTimedModule<string, int>(
                    new ConcurrencyLimitedModule<string, int>(
                        new LengthModule(),
                        moduleSemaphore,
                        maxConcurrency: 2),
                    3,
                    TimeSpan.FromMilliseconds(30000)),
                logger,
                activeJobs);

        // Build pipeline steps
        var steps = new List<IPipelineStep>
        {
            new PipelineStep<string, string>(uppercase),
            new PipelineStep<string, int>(length)
        };

        // Validate pipeline at startup (fail fast)
        PipelineValidator.Validate(steps, typeof(string));
        Console.WriteLine("Pipeline validation: OK");

        var orchestrator = new PipelineOrchestrator(steps);
        var globalSemaphore = new GlobalSemaphore(maxConcurrency: 2);
        var scheduler = new PipelineScheduler(orchestrator, globalSemaphore);

        using var cts = new CancellationTokenSource();

        var inputs = new[]
        {
            "example.com",
            "dotnet.microsoft.com",
            "github.com",
            "openai.com",
            "microsoft.com",
            "github.io",
            "example.com",
            "dotnet.microsoft.com",
            "github.com",
            "openai.com",
            "microsoft.com",
            "github.io",
            "example.com",
            "dotnet.microsoft.com",
            "github.com",
            "openai.com",
            "microsoft.com",
            "github.io",
            "example.com",
            "dotnet.microsoft.com",
            "github.com",
            "openai.com",
            "microsoft.com",
            "github.io",
            "example.com",
            "dotnet.microsoft.com",
            "github.com",
            "openai.com",
            "microsoft.com",
            "github.io"
        };

        var progress = new ProgressTracker(inputs.Length);

        var layout = new SpectreLayout(activeJobs, progress, logBuffer);

        using var uiCts = new CancellationTokenSource();

        await layout.RunAsync(async () =>
        {
            logger.Info("Pipeline execution started", Guid.Empty);
            layout.Refresh();

            var tasks = inputs.Select(input =>
            {
                var context = new PipelineContext
                {
                    InputId = input,
                    CancellationToken = cts.Token
                };

                activeJobs.Start(context.CorrelationId, input);
                layout.Refresh();

                return Task.Run(async () =>
                {
                    try
                    {
                        await scheduler.RunAsync(input, context);
                    }
                    finally
                    {
                        activeJobs.Complete(context.CorrelationId);
                        progress.MarkCompleted();
                        layout.Refresh();   // ← THIS is what you wanted
                    }
                });
            });

            await Task.WhenAll(tasks);

            uiCts.Cancel();
        }, uiCts.Token);
    }
}
