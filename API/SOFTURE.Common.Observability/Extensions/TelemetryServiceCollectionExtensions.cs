using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SOFTURE.Common.Observability.Export;
using SOFTURE.Common.Observability.Filters;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Selectors;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Extensions;

internal static class TelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddCommonTelemetry(this IServiceCollection services, IOpenTelemetrySettings telemetrySettings)
    {
        var settings = telemetrySettings.OpenTelemetry;
        var metricsSettings = settings.Metrics;
        var exportOrigins = ExportOrigins.From(telemetrySettings);
        var metricStreamSelector = new MetricStreamSelector(metricsSettings);

        services.AddSingleton(new PrometheusExporterSettings(settings.PrometheusEnabled));

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(settings.GetResourceAttributes()))
            .WithMetrics(metrics =>
            {
                if (metricsSettings.RuntimeEnabled)
                {
                    metrics.AddRuntimeInstrumentation();
                }

                if (metricsSettings.HttpClientEnabled)
                {
                    metrics
                        .AddHttpClientInstrumentation()
                        .AddRequestDurationRelay(
                            RequestDurationInstrument.HttpClientRequests,
                            new HttpClientRequestFilter(metricsSettings, exportOrigins),
                            metricsSettings);
                }

                if (metricsSettings.AspNetCoreEnabled)
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddRequestDurationRelay(
                            RequestDurationInstrument.HttpServerRequests,
                            new HttpServerRequestFilter(metricsSettings),
                            metricsSettings);
                }

                metrics
                    .AddView(metricStreamSelector.Select)
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
                        options.FilterHttpRequestMessage = request => !exportOrigins.Contains(request.RequestUri);
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddNpgsql()
                    .AddOtlpExporter(options => options.UseOpenTelemetrySettings(settings, OtlpSignal.Traces));
            });

        return services;
    }
}
