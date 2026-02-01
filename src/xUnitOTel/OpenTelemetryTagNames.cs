namespace xUnitOTel;

/// <summary>
/// Centralized OpenTelemetry tag name constants for test instrumentation.
/// </summary>
public static class OpenTelemetryTagNames
{
    public const string TestClassMethod = "test.class.method";
    public const string TestName = "test.name";
    public const string TestFramework = "test.framework";
    public const string TestRunId = "testrun.id";
    public const string TestFrameworkXUnit = "xunit";
}
