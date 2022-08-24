using System.Net;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Phobos.Actor;

namespace SqlSharding.Shared.Telemetry;

public static class TelemetryHostingExtensions
{
    /// <summary>
    ///     Adds monitoring and tracing support for any application that calls it.
    /// </summary>
    /// <param name="services">The services collection being configured by the application.</param>
    /// <returns>The original <see cref="IServiceCollection" /></returns>
    public static void AddPhobosApm(this IServiceCollection services)
    {
        var resource = ResourceBuilder.CreateDefault()
            .AddService(Assembly.GetEntryAssembly().GetName().Name, serviceInstanceId: $"{Dns.GetHostName()}");

        // enables OpenTelemetry for ASP.NET / .NET Core
        services.AddOpenTelemetryTracing(builder =>
        {
            builder
                .SetResourceBuilder(resource)
                .AddPhobosInstrumentation()
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddJaegerExporter(opt =>
                {
                        
                });
        });

        services.AddOpenTelemetryMetrics(builder =>
        {
            builder
                .SetResourceBuilder(resource)
                .AddPhobosInstrumentation()
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddPrometheusExporter(opt =>
                {
                });
        });
    }
}