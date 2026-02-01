using System.Diagnostics;
using System.Reflection;
using xUnitOTel.Diagnostics;
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

    private Activity? _activity;

    public bool CaptureError { get; set; } = true;
    public bool CaptureOut { get; set; } = true;

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        _ = ConsoleCaptureWriter.Value;

        var activityName = methodUnderTest.DeclaringType != null
            ? $"{methodUnderTest.DeclaringType.FullName}.{methodUnderTest.Name}"
            : methodUnderTest.Name;

        _activity = ApplicationDiagnostics.ActivitySource.StartActivity(activityName, ActivityKind.Internal);

        if (_activity is not null)
        {
            _activity.SetTag(OpenTelemetryTagNames.TestClassMethod, activityName);
            _activity.SetTag(OpenTelemetryTagNames.TestName, methodUnderTest.Name);
            _activity.SetTag(OpenTelemetryTagNames.TestFramework, OpenTelemetryTagNames.TestFrameworkXUnit);
        }
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (_activity is null) return;

        _activity.Stop();
        var traceId = _activity.TraceId.ToString();
        TestContextAccessor.Instance.Current?.TestOutputHelper?.WriteLine($"Trace ID: {traceId}");
        _activity.Dispose();
    }
}
