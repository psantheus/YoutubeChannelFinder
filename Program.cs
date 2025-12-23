using Spectre.Console;
using YoutubeChannelFinder.Core;
using YoutubeChannelFinder.Infrastructure.Concurrency;
using YoutubeChannelFinder.Infrastructure.Decorators;
using YoutubeChannelFinder.Infrastructure.Logging;
using YoutubeChannelFinder.Infrastructure.Progress;
using YoutubeChannelFinder.Infrastructure.Persistence;
using YoutubeChannelFinder.Infrastructure.UI;

namespace YoutubeChannelFinder;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("YoutubeChannelFinder – Smoke Test");
        Console.WriteLine("--------------------------------");

        // ===== Run / audit setup =====
        var runId = Guid.NewGuid();
        Console.WriteLine($"Pipeline RunId: {runId}");

        var auditWriter = new FileAuditWriter(
            root: "Outputs",
            runId: runId);

        // ===== Logging / UI =====
        var logBuffer = new LogBuffer();
        var logger = new ConsolePipelineLogger(logBuffer);
        var activeJobs = new ActiveJobTracker();

        // ===== Concurrency =====
        var moduleSemaphore = new ModuleSemaphore();
        var globalSemaphore = new GlobalSemaphore(maxConcurrency: 2);

        // ===== Modules (decorated) =====
        var uppercase =
            new LoggedTimedModule<string, string>(
                new RetryTimedModule<string, string>(
                    new ConcurrencyLimitedModule<string, string>(
                        new UppercaseModule(),
                        moduleSemaphore,
                        maxConcurrency: 2),
                    maxRetries: 3,
                    timeout: TimeSpan.FromSeconds(30)),
                logger,
                activeJobs);

        var length =
            new LoggedTimedModule<string, int>(
                new RetryTimedModule<string, int>(
                    new ConcurrencyLimitedModule<string, int>(
                        new LengthModule(),
                        moduleSemaphore,
                        maxConcurrency: 2),
                    maxRetries: 3,
                    timeout: TimeSpan.FromSeconds(30)),
                logger,
                activeJobs);

        // ===== Pipeline steps =====
        var steps = new List<IPipelineStep>
        {
            new PipelineStep<string, string>(uppercase),
            new PipelineStep<string, int>(length)
        };

        PipelineValidator.Validate(steps, typeof(string));
        Console.WriteLine("Pipeline validation: OK");

        // ===== Orchestrator + Scheduler =====
        var orchestrator = new PipelineOrchestrator(
            steps,
            auditWriter);

        var scheduler = new PipelineScheduler(
            orchestrator,
            globalSemaphore);

        // ===== Inputs =====
        var inputs = new[]
        {
            "example.com",
            "dotnet.microsoft.com",
            "github.com",
            "openai.com",
            "microsoft.com",
            "github.io"
        };

        var progress = new ProgressTracker(inputs.Length);

        var layout = new SpectreLayout(
            activeJobs,
            progress,
            logBuffer);

        using var cts = new CancellationTokenSource();
        using var uiCts = new CancellationTokenSource();

        // ===== UI + execution =====
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
                        // IMPORTANT: orchestrator now RETURNS a value
                        await scheduler.RunAsync<string, int>(input, context);
                    }
                    finally
                    {
                        activeJobs.Complete(context.CorrelationId);
                        progress.MarkCompleted();
                        layout.Refresh();
                    }
                });
            });

            await Task.WhenAll(tasks);

            uiCts.Cancel();
        }, uiCts.Token);
    }
}
