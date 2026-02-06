using System.Diagnostics;
using System.Reflection;
using xUnitOTel.Diagnostics;
using xUnitOTel.Logging;
using Xunit.Internal;
using Xunit.v3;

namespace xUnitOTel.Attributes;

/// <summary>
/// xUnit attribute that wraps test methods in OpenTelemetry spans for distributed tracing.
/// Can be applied at assembly level [assembly: Trace] or per-test method.
/// </summary>
public class TraceAttribute : BeforeAfterTestAttribute
{

    private static readonly Lazy<ConsoleCaptureTestOutputWriter> ConsoleCaptureWriter = new(() =>
        new ConsoleCaptureTestOutputWriter(TestContextAccessor.Instance, captureError: true, captureOut: true),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly AsyncLocal<Activity?> _activity = new();

    public bool CaptureError { get; set; } = true;
    public bool CaptureOut { get; set; } = true;

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        _ = ConsoleCaptureWriter.Value;

        var activityName = methodUnderTest.DeclaringType != null
            ? $"{methodUnderTest.DeclaringType.FullName}.{methodUnderTest.Name}"
            : methodUnderTest.Name;

        _activity.Value = ApplicationDiagnostics.ActivitySource.StartActivity(activityName, ActivityKind.Internal);

        if (_activity.Value is not null)
        {
            _activity.Value.SetTag(OpenTelemetryTagNames.TestClassMethod, activityName);
            _activity.Value.SetTag(OpenTelemetryTagNames.TestName, methodUnderTest.Name);
            _activity.Value.SetTag(OpenTelemetryTagNames.TestFramework, OpenTelemetryTagNames.TestFrameworkXUnit);
        }
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        var activity = _activity.Value;
        if (activity is null) return;

        activity.Stop();
        var traceId = activity.TraceId.ToString();
        TestContextAccessor.Instance.Current?.TestOutputHelper?.WriteLine($"Trace ID: {traceId}");

        var testState = Xunit.TestContext.Current?.TestState;
        var testFailed = testState?.Result == Xunit.TestResult.Failed;

        if (testFailed && testState != null)
        {
            FailedTestLogExporter.Instance.ExportToFile(
                traceId,
                methodUnderTest.Name,
                methodUnderTest.DeclaringType?.Name,
                testState.ExceptionMessages,
                testState.ExceptionStackTraces);
        }
        else
        {
            FailedTestLogExporter.Instance.Clear(traceId);
        }

        activity.Dispose();
        _activity.Value = null;
    }
}
