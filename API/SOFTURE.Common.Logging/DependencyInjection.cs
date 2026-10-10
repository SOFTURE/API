using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SOFTURE.Common.HealthCheck;
using SOFTURE.Common.Logging.Extensions;
using SOFTURE.Common.Logging.HealthChecks;
using SOFTURE.Common.Logging.Settings;
using SOFTURE.Common.Otlp.Settings;
using SOFTURE.Settings.Extensions;

namespace SOFTURE.Common.Logging;

public static class DependencyInjection
{
    private static string ApplicationName() => Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";

    public static IServiceCollection AddCommonLogging<TSettings>(this IServiceCollection services)
        where TSettings : IOpenTelemetrySettings
    {
        var settings = services.GetSettings<TSettings, TSettings>(x => x);

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", $"{ApplicationName()}")
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteToOpenTelemetry(settings.OpenTelemetry);

        if (settings is ISeqSettings seqSettings)
        {
            loggerConfiguration.WriteToSeq(seqSettings.Seq);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        Log.Information("{ApplicationName} - Application starting up", ApplicationName());

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
