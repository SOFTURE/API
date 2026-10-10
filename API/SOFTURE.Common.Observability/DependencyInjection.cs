using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SOFTURE.Common.Observability.Exporters;
using SOFTURE.Common.Observability.Settings;
using SOFTURE.Common.Otlp;
using SOFTURE.Settings.Extensions;

namespace SOFTURE.Common.Observability;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonObservability<TSettings>(this IServiceCollection services)
        where TSettings : IObservabilitySettings
    {
        var settings = services.GetSettings<TSettings, ObservabilitySettings?>(x => x.Observability);

        return services.AddCommonObservabilityCore(settings?.Url);
    }

    public static IServiceCollection AddCommonObservability(this IServiceCollection services)
    {
        return services.AddCommonObservabilityCore(legacyUrl: null);
    }

    public static WebApplication UseCommonOpenTelemetry(this WebApplication app)
    {
        var options = app.Services.GetService<CommonObservabilityOptions>();

        if (options?.PrometheusEnabled ?? true)
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        return app;
    }

    private static IServiceCollection AddCommonObservabilityCore(this IServiceCollection services, string? legacyUrl)
    {
        var otlp = OtlpConfiguration.FromServices(services);
        var plan = ObservabilityExportPlan.Create(otlp, legacyUrl);
        var resourceAttributes = otlp.ResolveResourceAttributes();

        services.AddSingleton(new CommonObservabilityOptions(otlp.PrometheusEnabled));

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(resourceAttributes))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                if (otlp.PrometheusEnabled)
                {
                    metrics.AddPrometheusExporter();
                }

                if (plan.Metrics is not null)
                {
                    metrics.AddOtlpExporter(otlpOptions => plan.Metrics.ApplyTo(otlpOptions));
                }
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(DiagnosticConfig.MassTransitSource)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.FilterHttpRequestMessage = request => !plan.IsExportRequest(request.RequestUri);
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddNpgsql();

                if (plan.Traces is not null)
                {
                    tracing.AddOtlpExporter(otlpOptions => plan.Traces.ApplyTo(otlpOptions));
                }
            });

        return services;
    }
}
