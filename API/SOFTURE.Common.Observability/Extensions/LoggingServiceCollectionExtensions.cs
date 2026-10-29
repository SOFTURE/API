using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SOFTURE.Common.HealthCheck;
using SOFTURE.Common.Observability.HealthChecks;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Extensions;

internal static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddCommonLogging(this IServiceCollection services, IOpenTelemetrySettings settings)
    {
        var applicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteToOpenTelemetry(settings.OpenTelemetry);

        if (settings is ISeqSettings seqSettings)
        {
            loggerConfiguration.WriteToSeq(seqSettings.Seq);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        Log.Information("{ApplicationName} - Application starting up", applicationName);

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });

        if (settings is ISeqSettings)
        {
            services.AddCommonHealthCheck<SeqHealthCheck>();
        }

        return services;
    }
}
