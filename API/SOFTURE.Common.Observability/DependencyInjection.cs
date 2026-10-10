using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SOFTURE.Common.Observability.Extensions;
using SOFTURE.Common.Observability.Settings;
using SOFTURE.Common.Otlp.Extensions;
using SOFTURE.Common.Otlp.Models;
using SOFTURE.Common.Otlp.Settings;
using SOFTURE.Settings.Extensions;

namespace SOFTURE.Common.Observability;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonObservability<TSettings>(this IServiceCollection services)
        where TSettings : IOpenTelemetrySettings
    {
        var settings = services.GetSettings<TSettings, OpenTelemetrySettings>(x => x.OpenTelemetry);

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

    public static WebApplication UseCommonOpenTelemetry(this WebApplication app)
    {
        var prometheus = app.Services.GetRequiredService<PrometheusExporterSettings>();

        if (prometheus.IsEnabled)
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        return app;
    }
}
