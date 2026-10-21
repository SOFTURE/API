using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SOFTURE.Common.Observability.Extensions;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Telemetry;

internal static class TelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddCommonTelemetry(this IServiceCollection services, OpenTelemetrySettings settings)
    {
        services.AddSingleton(new PrometheusExporterSettings(settings.PrometheusEnabled));

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(settings.GetResourceAttributes()))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddView(instrument => MetricStreamSelector.Select(instrument, settings.Metrics))
                    .AddOtlpExporter(options => options.UseOpenTelemetrySettings(settings, OtlpSignal.Metrics));

                if (settings.PrometheusEnabled)
                {
                    metrics.AddPrometheusExporter();
                }
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(DiagnosticConfig.MassTransitSource)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.FilterHttpRequestMessage = request => !settings.IsExportRequest(request.RequestUri);
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddNpgsql()
                    .AddOtlpExporter(options => options.UseOpenTelemetrySettings(settings, OtlpSignal.Traces));
            });

        return services;
    }
}
