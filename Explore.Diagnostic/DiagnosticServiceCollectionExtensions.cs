using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Explore.Diagnostic
{
    public static class DiagnosticServiceCollectionExtensions
    {
        public static IServiceCollection AddObservability(this IServiceCollection services,
            string serviceName,
            IConfiguration configuration,
            string[]? meeterNames = null)
        {
            // create the resource that references the servivce name passed in
            var resource = ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: "1.0");

            // add the OpenTelemetry services
            var openTelemetryBuilder = services.AddOpenTelemetry();

            openTelemetryBuilder
                .WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(resource)
                        .AddRuntimeInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddEventCountersInstrumentation(c =>
                        {
                            c.AddEventSources(
                                "Microsoft.AspNetCore.Hosting",
                                "Microsoft-AspNetCore-Server-Kestrel",
                                "System.Net.Http",
                                "System.Net.Sockets"
                            );
                        })
                        .AddView("request-duration", new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
                        })
                        .AddMeter("Miscrost.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel")
                        .AddPrometheusExporter()
                        .AddConsoleExporter();

                    // add any additional meters provided by the caller
                    if (meeterNames != null)
                    {
                        foreach (var name in meeterNames)
                        {
                            metrics.AddMeter(name);
                        }
                    }
                })
                .WithTracing(tracing =>
                {
                    tracing.SetResourceBuilder(resource)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddZipkinExporter(zipkin =>
                        {
                            var zipkinUrl = configuration["ZIPKIN_URL"] ?? "http://zipkin:9411";
                            zipkin.Endpoint = new Uri($"{zipkinUrl}/api/v2/spans");
                        });
                    //.AddSqlClientInstrumentation(); TODO I may not need this, it was in microsoft course, need to further investigate and research this
                });
            return services;
        }

        // Add the Prometheus endpoints to your service, this will expose the metrics at http://service/metrics
        public static void MapObservability(this IEndpointRouteBuilder routes)
        {
            routes.MapPrometheusScrapingEndpoint();
        }
    }
}
