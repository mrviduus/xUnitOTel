using System.Collections.Concurrent;
using System.Text.Json;

namespace xUnitOTel.Logging;

public class FailedTestLogExporter
{
    public static FailedTestLogExporter Instance { get; } = new();

    private readonly ConcurrentDictionary<string, List<FailedTestLogEntry>> _logsByTraceId = new();
    private FailedTestLogOptions _options = new();
    private readonly object _lock = new();

    private FailedTestLogExporter() { }

    public void Configure(FailedTestLogOptions options)
    {
        _options = options;
    }

    public void AddLog(string traceId, FailedTestLogEntry entry)
    {
        if (!_options.Enabled) return;

        _logsByTraceId.AddOrUpdate(
            traceId,
            _ => new List<FailedTestLogEntry> { entry },
            (_, list) =>
            {
                lock (_lock)
                {
                    list.Add(entry);
                }
                return list;
            });
    }

    public void ExportToFile(
        string traceId,
        string testName,
        string? testClass,
        string?[]? exceptionMessages = null,
        string?[]? stackTraces = null)
    {
        if (!_options.Enabled) return;
        if (!_logsByTraceId.TryRemove(traceId, out var logs) || logs.Count == 0) return;

        var timestamp = DateTime.UtcNow;
        var output = new
        {
            traceId,
            testClass,
            testName,
            timestamp = timestamp.ToString("o"),
            failure = exceptionMessages != null ? new
            {
                messages = exceptionMessages,
                stackTraces
            } : null,
            logs
        };

        var fileName = $"{testClass}.{testName}_{timestamp:yyyyMMdd_HHmmss}.json";
        var directory = _options.OutputDirectory;

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var filePath = Path.Combine(directory, fileName);
        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        File.WriteAllText(filePath, json);
    }

    public void Clear(string traceId)
    {
        _logsByTraceId.TryRemove(traceId, out _);
    }

    // For testing purposes
    internal bool HasLogs(string traceId) => _logsByTraceId.ContainsKey(traceId);
}
