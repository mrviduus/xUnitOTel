// Import Microsoft dependency injection services for service collection extensions
using Microsoft.Extensions.DependencyInjection;
// Import TryAdd extension methods for safe service registration
using Microsoft.Extensions.DependencyInjection.Extensions;
// Import Microsoft logging framework for logging configuration
using Microsoft.Extensions.Logging;
// Import core OpenTelemetry functionality
using OpenTelemetry;
// Import OpenTelemetry logging extensions
using OpenTelemetry.Logs;
// Import OpenTelemetry metrics functionality
using OpenTelemetry.Metrics;
// Import OpenTelemetry resource management
using OpenTelemetry.Resources;
// Import OpenTelemetry tracing functionality
using OpenTelemetry.Trace;
// Import custom logging implementations for xUnit integration
using xUnitOTel.Logging;
// Import custom processors for OpenTelemetry data processing
using xUnitOTel.Processors;

// Define the namespace for OpenTelemetry configuration extensions
namespace xUnitOTel.Diagnostics;

// Static class containing extension methods for configuring OpenTelemetry diagnostics in xUnit tests
// This class provides a fluent API for setting up tracing, metrics, and logging with minimal configuration
public static class OTelConfigurationExtensions
{
    // Main extension method that configures OpenTelemetry diagnostics for the service collection
    // This method sets up tracing, metrics, and logging with sensible defaults while allowing customization
    public static IServiceCollection AddOTelDiagnostics(
        this IServiceCollection services,
        // Optional action to configure the OpenTelemetry resource builder (adds metadata like service name, version)
        Action<ResourceBuilder>? configureResourceBuilder = null,
        // Optional action to configure the metrics provider builder (adds custom meters, exporters)
        Action<MeterProviderBuilder>? configureMeterProviderBuilder = null,
        // Optional action to configure the tracer provider builder (adds custom sources, exporters)
        Action<TracerProviderBuilder>? configureTracerProviderBuilder = null,
        // Optional action to configure the logging builder (adds custom providers, filters)
        Action<ILoggingBuilder>? configureLoggingBuilder = null)
    {
        // Register the test output helper accessor as a singleton service for dependency injection
        // This service provides access to xUnit's ITestOutputHelper for logging test output
        services.TryAddSingleton<ITestOutputHelperAccessor, TestOutputHelperAccessor>();
        // Register xUnit's test context accessor to enable access to current test context
        services.TryAddSingleton<Xunit.ITestContextAccessor>(_ => Xunit.v3.TestContextAccessor.Instance);

        // Configure OpenTelemetry with the specified activity source name and custom configuration actions
        // This sets up the core OpenTelemetry services with tracing, metrics, and resource configuration
        services.AddOpenTelemetry().ConfigureOpenTelemetry(
            ApplicationDiagnostics.ActivitySourceName,
            configureResourceBuilder,
            configureMeterProviderBuilder,
            configureTracerProviderBuilder);

        // Configure OpenTelemetry logging integration with custom logging providers and processors
        services.ConfigureOpenTelemetryLogging(configureLoggingBuilder);

        // Return the service collection to enable method chaining
        return services;
    }

    // Private extension method that configures the core OpenTelemetry builder with resource, metrics, and tracing
    // This method centralizes the configuration of OpenTelemetry components and applies custom configurations
    private static OpenTelemetryBuilder ConfigureOpenTelemetry(
        this OpenTelemetryBuilder builder,
        // The source name used to identify activities from this library
        string sourceName,
        // Optional action to customize resource configuration (service name, version, environment)
        Action<ResourceBuilder>? configureResource,
        // Optional action to customize metrics configuration (additional meters, exporters)
        Action<MeterProviderBuilder>? configureMetrics,
        // Optional action to customize tracing configuration (additional sources, samplers, exporters)
        Action<TracerProviderBuilder>? configureTracing)
    {
        // Configure the OpenTelemetry builder with resource, metrics, and tracing components
        builder
            // Configure the resource that describes this service instance
            .ConfigureResource(resource =>
            {
                // Add the service name to the resource attributes for identification in telemetry backends
                resource.AddService(ApplicationDiagnostics.ActivitySourceName);
                // Apply any custom resource configuration provided by the caller
                configureResource?.Invoke(resource);
            })
            // Configure metrics collection and export
            .WithMetrics(metrics =>
            {
                // Apply default metrics configuration for this library
                metrics.ConfigureDefaultMetrics(sourceName);
                // Apply any custom metrics configuration provided by the caller
                configureMetrics?.Invoke(metrics);
            })
            // Configure distributed tracing collection and export
            .WithTracing(tracing =>
            {
                // Apply default tracing configuration for this library
                tracing.ConfigureDefaultTracing(sourceName);
                // Apply any custom tracing configuration provided by the caller
                configureTracing?.Invoke(tracing);
            });

        // Return the configured builder to enable method chaining
        return builder;
    }

    // Private extension method that configures default metrics collection for xUnitOTel
    // This method sets up standard metrics instrumentation suitable for test scenarios
    private static void ConfigureDefaultMetrics(
        this MeterProviderBuilder metrics,
        // The name of the meter source to collect metrics from
        string sourceName)
    {
        // Configure the metrics provider with default instrumentation
        metrics
            // Add the custom meter for this library to collect application-specific metrics
            .AddMeter(sourceName)
            // Add .NET runtime instrumentation to collect GC, thread pool, and JIT metrics
            .AddRuntimeInstrumentation();
    }

    // Private extension method that configures default distributed tracing for xUnitOTel
    // This method sets up instrumentation for common libraries and frameworks used in tests
    private static void ConfigureDefaultTracing(
        this TracerProviderBuilder tracing,
        // The name of the activity source to trace activities from
        string sourceName)
    {
        // Configure the tracer provider with comprehensive instrumentation
        tracing
            // Add HTTP client instrumentation to trace outgoing HTTP requests and record exceptions
            .AddHttpClientInstrumentation(o => o.RecordException = true)
            // Add SQL client instrumentation to trace database operations
            .AddSqlClientInstrumentation()
            // Add gRPC client instrumentation to trace gRPC calls
            .AddGrpcClientInstrumentation()
            // Set sampling strategy to always collect traces (useful for test scenarios)
            .SetSampler(new AlwaysOnSampler())
            // Add the custom activity source for this library to the tracer
            .AddSource(sourceName)
            // Add custom processor to inject test run ID into all activities
            .AddProcessor(new TestRunIdProcessor());

    }

    // Private extension method that configures OpenTelemetry logging integration
    // This method sets up logging providers and processors to integrate with OpenTelemetry
    private static IServiceCollection ConfigureOpenTelemetryLogging(
        this IServiceCollection services,
        // Optional action to customize logging configuration
        Action<ILoggingBuilder>? configureLogging)
    {
        // Configure the logging system with OpenTelemetry integration
        services.AddLogging(logging =>
        {
            // Remove default logging providers to have full control over logging configuration
            logging.ClearProviders();

            // Add OpenTelemetry logging provider with comprehensive configuration
            logging.AddOpenTelemetry(options =>
            {
                // Include the formatted log message in the telemetry data
                options.IncludeFormattedMessage = true;
                // Include logging scopes in the telemetry data for better context
                options.IncludeScopes = true;
                // Parse state values to extract structured data from log entries
                options.ParseStateValues = true;
                // Add custom processor to attach log entries as events to the current activity
                options.AddProcessor(new ActivityEventLogProcessor());
                // Only add OTLP exporter in debug builds to reduce overhead in production tests
            });

            // Add debug logging provider for development scenarios
            logging.AddDebug();
            // Add console logging provider for visible output during test runs
            logging.AddConsole();
            // Add custom xUnit output logging provider to integrate with xUnit's test output
            logging.AddXUnitOutput();

            configureLogging?.Invoke(logging);
        });
        // Return the service collection to enable method chaining
        return services;
    }
}
