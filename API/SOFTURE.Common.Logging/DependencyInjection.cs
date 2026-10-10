using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SOFTURE.Common.HealthCheck;
using SOFTURE.Common.Logging.Extensions;
using SOFTURE.Common.Logging.HealthChecks;
using SOFTURE.Common.Logging.Settings;
using SOFTURE.Common.Otlp;
using SOFTURE.Settings.Extensions;

namespace SOFTURE.Common.Logging;

public static class DependencyInjection
{
    private static string ApplicationName() => Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";

    public static IServiceCollection AddCommonLogging<TSettings>(this IServiceCollection services)
        where TSettings : ISeqSettings
    {
        var seqSettings = services.GetSettings<TSettings, SeqSettings?>(x => x.Seq);

        return services.AddCommonLoggingCore(seqSettings);
    }

    public static IServiceCollection AddCommonLogging(this IServiceCollection services)
    {
        return services.AddCommonLoggingCore(seqSettings: null);
    }

    private static IServiceCollection AddCommonLoggingCore(this IServiceCollection services, SeqSettings? seqSettings)
    {
        var otlp = OtlpConfiguration.FromServices(services);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", $"{ApplicationName()}")
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteToSeqIfConfigured(seqSettings)
            .WriteToOtlpIfConfigured(otlp)
            .CreateLogger();

        Log.Information("{ApplicationName} - Application starting up", ApplicationName());

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });

        if (seqSettings.IsConfigured())
        {
            services.AddCommonHealthCheck<SeqHealthCheck>();
        }

        return services;
    }
}
