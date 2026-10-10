using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SOFTURE.Common.Observability.Logging;
using SOFTURE.Common.Observability.Settings;
using SOFTURE.Common.Observability.Telemetry;
using SOFTURE.Settings.Extensions;

namespace SOFTURE.Common.Observability;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonObservability<TSettings>(this IServiceCollection services)
        where TSettings : IOpenTelemetrySettings
    {
        var settings = services.GetSettings<TSettings, TSettings>(x => x);

        return services
            .AddCommonLogging(settings)
            .AddCommonTelemetry(settings.OpenTelemetry);
    }

    public static WebApplication UseCommonObservability(this WebApplication app)
    {
        var prometheus = app.Services.GetRequiredService<PrometheusExporterSettings>();

        if (prometheus.IsEnabled)
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        return app;
    }
}
