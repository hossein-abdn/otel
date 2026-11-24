using System.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "dotnet-test-api";
const string activitySourceName = "OtelTestApi";

var activitySource = new ActivitySource(activitySourceName);

// OpenTelemetry configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: serviceName,
            serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(activitySourceName)
        .AddOtlpExporter(options =>
        {
            options.Endpoint =
                new Uri("http://otel-collector:4317");

            options.Protocol =
                OtlpExportProtocol.Grpc;
        }));

var app = builder.Build();

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        Message = "OTel Test API",
        Service = serviceName
    });
});

app.MapGet("/api/test", () =>
{
    using var activity = activitySource.StartActivity(
        "custom-test-operation",
        ActivityKind.Internal);

    activity?.SetTag("test.name", "my-otel-test");
    activity?.SetTag("test.environment", "development");

    return Results.Ok(new
    {
        Message = "OpenTelemetry test successful",

        TraceId = Activity.Current?.TraceId.ToString(),
        SpanId = Activity.Current?.SpanId.ToString(),

        CustomSpanId = activity?.SpanId.ToString(),

        ParentSpanId = activity?.ParentSpanId.ToString()
    });
});

app.MapGet("/api/error", () =>
{
    using var activity = activitySource.StartActivity(
        "custom-error-operation");

    try
    {
        throw new InvalidOperationException(
            "This is a test exception");
    }
    catch (Exception ex)
    {
        activity?.SetStatus(
            ActivityStatusCode.Error,
            ex.Message);

        activity?.RecordException(ex);

        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
});

app.Run();