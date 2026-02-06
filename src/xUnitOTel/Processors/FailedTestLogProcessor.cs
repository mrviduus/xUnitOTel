using OpenTelemetry;
using OpenTelemetry.Logs;
using xUnitOTel.Logging;

namespace xUnitOTel.Processors;

public class FailedTestLogProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord? data)
    {
        if (data == null) return;

        base.OnEnd(data);

        var traceId = data.TraceId.ToString();
        if (string.IsNullOrEmpty(traceId) || traceId == "00000000000000000000000000000000") return;

        var entry = new FailedTestLogEntry
        {
            Timestamp = data.Timestamp.ToString("o"),
            Level = data.LogLevel.ToString(),
            Category = data.CategoryName ?? string.Empty,
            Message = data.FormattedMessage ?? data.Body ?? string.Empty,
            Exception = data.Exception?.ToString()
        };

        FailedTestLogExporter.Instance.AddLog(traceId, entry);
    }
}
