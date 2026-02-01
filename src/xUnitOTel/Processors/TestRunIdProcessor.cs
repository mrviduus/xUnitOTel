using System.Diagnostics;
using OpenTelemetry;

namespace xUnitOTel.Processors;

/// <summary>
/// Processor that adds a unique test run identifier to all activities.
/// Enables correlation of all spans from a single test execution session.
/// </summary>
public class TestRunIdProcessor : BaseProcessor<Activity>
{
    private static readonly Guid TestRunId = Guid.NewGuid();

    public override void OnStart(Activity data)
    {
        data.SetTag(OpenTelemetryTagNames.TestRunId, TestRunId.ToString());
    }
}
