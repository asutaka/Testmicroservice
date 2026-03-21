using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace Observability.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        string jaegerHost = Environment.GetEnvironmentVariable("JAEGER_HOST") ?? "localhost";
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                    })
                    .AddGrpcClientInstrumentation()  // from OpenTelemetry.Instrumentation.GrpcNetClient
                    .AddJaegerExporter(o =>
                    {
                        o.AgentHost = jaegerHost;
                        o.AgentPort = 6831;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }

    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        return app;
    }
}
