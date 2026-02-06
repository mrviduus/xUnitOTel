using System.Text.Json;
using xUnitOTel.Logging;

namespace xUnitOTel.Tests;

[Collection("ExporterTests")]
public class FailedTestLogExporterTests : IDisposable
{
    private readonly string _testOutputDir;

    public FailedTestLogExporterTests()
    {
        // Reset singleton to defaults before each test
        FailedTestLogExporter.Instance.Configure(new FailedTestLogOptions());
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"test-output-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        // Reset singleton options to defaults
        FailedTestLogExporter.Instance.Configure(new FailedTestLogOptions());

        if (Directory.Exists(_testOutputDir))
        {
            Directory.Delete(_testOutputDir, recursive: true);
        }
    }

    [Fact]
    public void AddLog_StoresLogByTraceId()
    {
        var traceId = Guid.NewGuid().ToString("N");
        var entry = new FailedTestLogEntry { Message = "test log" };

        FailedTestLogExporter.Instance.AddLog(traceId, entry);

        Assert.True(FailedTestLogExporter.Instance.HasLogs(traceId));

        // Cleanup
        FailedTestLogExporter.Instance.Clear(traceId);
    }

    [Fact]
    public void ExportToFile_CreatesJsonFile_WhenLogsExist()
    {
        var options = new FailedTestLogOptions { OutputDirectory = _testOutputDir };
        FailedTestLogExporter.Instance.Configure(options);
        var traceId = Guid.NewGuid().ToString("N");
        FailedTestLogExporter.Instance.AddLog(traceId, new FailedTestLogEntry { Message = "test" });

        FailedTestLogExporter.Instance.ExportToFile(traceId, "TestMethod", "TestClass");

        var files = Directory.GetFiles(_testOutputDir, "*.json");
        Assert.NotEmpty(files);
    }

    [Fact]
    public void ExportToFile_DoesNothing_WhenNoLogs()
    {
        var options = new FailedTestLogOptions { OutputDirectory = _testOutputDir };
        FailedTestLogExporter.Instance.Configure(options);
        var traceId = Guid.NewGuid().ToString("N");

        // No logs added for this traceId
        FailedTestLogExporter.Instance.ExportToFile(traceId, "TestMethod", "TestClass");

        Assert.False(Directory.Exists(_testOutputDir) && Directory.GetFiles(_testOutputDir, "*.json").Length > 0);
    }

    [Fact]
    public void Clear_RemovesLogsForTraceId()
    {
        var traceId = Guid.NewGuid().ToString("N");
        FailedTestLogExporter.Instance.AddLog(traceId, new FailedTestLogEntry { Message = "test" });

        FailedTestLogExporter.Instance.Clear(traceId);

        Assert.False(FailedTestLogExporter.Instance.HasLogs(traceId));
    }

    [Fact]
    public void JsonFormat_ContainsExpectedFields()
    {
        var options = new FailedTestLogOptions { OutputDirectory = _testOutputDir };
        FailedTestLogExporter.Instance.Configure(options);
        var traceId = Guid.NewGuid().ToString("N");
        var entry = new FailedTestLogEntry
        {
            Timestamp = DateTime.UtcNow.ToString("o"),
            Level = "Information",
            Category = "TestCategory",
            Message = "Test message",
            Exception = null
        };
        FailedTestLogExporter.Instance.AddLog(traceId, entry);

        FailedTestLogExporter.Instance.ExportToFile(traceId, "TestMethod", "TestClass");

        var files = Directory.GetFiles(_testOutputDir, "*.json");
        Assert.Single(files);

        var json = File.ReadAllText(files[0]);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(traceId, root.GetProperty("traceId").GetString());
        Assert.Equal("TestClass", root.GetProperty("testClass").GetString());
        Assert.Equal("TestMethod", root.GetProperty("testName").GetString());
        Assert.True(root.TryGetProperty("timestamp", out _));
        Assert.True(root.TryGetProperty("logs", out var logs));
        Assert.Equal(JsonValueKind.Array, logs.ValueKind);
        Assert.Equal(1, logs.GetArrayLength());
    }
}
