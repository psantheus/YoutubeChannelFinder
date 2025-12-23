using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using YoutubeChannelFinder.Infrastructure.Logging;
using YoutubeChannelFinder.Infrastructure.Progress;

namespace YoutubeChannelFinder.Infrastructure.UI;

public sealed class SpectreLayout
{
    private readonly ActiveJobTracker _activeJobs;
    private readonly ProgressTracker _progress;
    private readonly LogBuffer _logBuffer;

    private readonly SemaphoreSlim _refreshSignal = new(0, int.MaxValue);

    private Layout? _layout;
    private LiveDisplayContext? _ctx;

    public SpectreLayout(
        ActiveJobTracker activeJobs,
        ProgressTracker progress,
        LogBuffer logBuffer)
    {
        _activeJobs = activeJobs;
        _progress = progress;
        _logBuffer = logBuffer;
    }

    /// <summary>
    /// Call this whenever progress or logs change.
    /// </summary>
    public void Refresh()
    {
        _refreshSignal.Release();
    }

    public async Task RunAsync(Func<Task> workload, CancellationToken token)
    {
        _layout = new Layout("root")
            .SplitRows(
                new Layout("logs"),
                new Layout("dashboard").Size(12));

        await AnsiConsole.Live(_layout)
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                _ctx = ctx;

                // Initial render
                UpdateAll();

                var refreshTask = Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        await _refreshSignal.WaitAsync(token);
                        UpdateAll();
                    }
                }, token);

                await workload();

                UpdateAll();

                try { await refreshTask; }
                catch (OperationCanceledException) { }
            });
    }

    private void UpdateAll()
    {
        _layout!["logs"].Update(BuildLogsPanel().Expand());
        _layout!["dashboard"].Update(BuildDashboard().Expand());
        _ctx!.Refresh();
    }

    private Panel BuildLogsPanel()
    {
        var lines = _logBuffer.Snapshot();

        var content = lines.Count == 0
            ? new Markup("[grey]No logs yet[/]")
            : new Markup(string.Join("\n", lines.Select(Markup.Escape)));

        return new Panel(content)
        {
            Header = new PanelHeader("Logs"),
            Border = BoxBorder.Rounded
        }.Expand();
    }

    private Panel BuildDashboard()
    {
        var percent = _progress.Total == 0
            ? 1
            : (double)_progress.Completed / _progress.Total;

        return new Panel(
            new Rows(
                new Markup(
                    $"[bold]Progress[/]\n" +
                    $"{_progress.Completed} / {_progress.Total} ({percent:P0})\n" +
                    $"Elapsed: {_progress.Elapsed:hh\\:mm\\:ss} | Active: {_activeJobs.ActiveCount}"
                ),
                new Rule(),
                BuildActiveTable()
            ))
        {
            Header = new PanelHeader("Pipeline Status"),
            Border = BoxBorder.Rounded
        }.Expand();
    }

    private Table BuildActiveTable()
    {
        var table = new Table()
            .AddColumn("Input")
            .AddColumn("Stage")
            .AddColumn("Elapsed");

        foreach (var (input, stage, elapsed) in _activeJobs.Snapshot())
        {
            table.AddRow(input, stage, elapsed.ToString(@"mm\:ss"));
        }

        return table;
    }
}
