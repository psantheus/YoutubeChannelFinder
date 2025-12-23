using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace YoutubeChannelFinder.Infrastructure.Persistence;

public sealed class FileAuditWriter
{
    private readonly string _root;
    private readonly Guid _runId;

    public FileAuditWriter(string root, Guid runId)
    {
        _root = root;
        _runId = runId;
    }

    public void WriteModuleInput(
        string inputId,
        string moduleName,
        object input)
    {
        var dir = GetModuleDir(inputId, moduleName);
        Directory.CreateDirectory(dir);

        WriteJson(Path.Combine(dir, "input.json"), input);
    }

    public void WriteModuleSuccess(
        string inputId,
        string moduleName,
        object output)
    {
        var dir = GetModuleDir(inputId, moduleName);

        WriteOutput(dir, output);
        WriteStatus(dir, new { status = "Success" });
    }

    public void WriteModuleFailure(
        string inputId,
        string moduleName,
        Exception ex)
    {
        var dir = GetModuleDir(inputId, moduleName);
        Directory.CreateDirectory(dir);

        WriteStatus(dir, new
        {
            status = "Failed",
            errorType = ex is OperationCanceledException ? "Timeout" : ex.GetType().Name,
            message = ex.Message
        });
    }

    public void WriteInputSummary(
        string inputId,
        IReadOnlyList<string> succeededModules,
        string? failedModule,
        Exception? error)
    {
        var dir = GetInputDir(inputId);
        Directory.CreateDirectory(dir);

        WriteJson(Path.Combine(dir, "_summary.json"), new
        {
            inputId,
            status = failedModule == null ? "Success" : "Failed",
            modulesExecuted = succeededModules,
            failedModule,
            error = error == null ? null : new
            {
                type = error is OperationCanceledException ? "Timeout" : error.GetType().Name,
                message = error.Message
            }
        });
    }

    private string GetInputDir(string inputId) =>
        Path.Combine(_root, _runId.ToString(), Sanitize(inputId));

    private string GetModuleDir(string inputId, string moduleName) =>
        Path.Combine(GetInputDir(inputId), moduleName);

    private static void WriteOutput(string dir, object output)
    {
        if (output is string s)
            File.WriteAllText(Path.Combine(dir, "output.txt"), s);
        else
            WriteJson(Path.Combine(dir, "output.json"), output);
    }

    private static void WriteStatus(string dir, object status) =>
        WriteJson(Path.Combine(dir, "status.json"), status);

    private static void WriteJson(string path, object obj) =>
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));

    private static string Sanitize(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        return value;
    }
}
