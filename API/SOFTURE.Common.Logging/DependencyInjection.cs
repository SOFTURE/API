using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using SOFTURE.Common.HealthCheck;
using SOFTURE.Common.Logging.HealthChecks;
using SOFTURE.Common.Logging.Settings;
using SOFTURE.Common.Otlp;
using SOFTURE.Settings.Extensions;

namespace SOFTURE.Common.Logging;

public static class DependencyInjection
{
    private static string ApplicationName() => Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";

    /// <summary>
    /// Configures Serilog with the console sink, the Seq sink (when <c>Seq:Url</c> is set) and the OTLP sink
    /// (when the <c>OpenTelemetry</c> section or the standard <c>OTEL_EXPORTER_OTLP_*</c> variables name an endpoint).
    /// </summary>
    public static IServiceCollection AddCommonLogging<TSettings>(this IServiceCollection services)
        where TSettings : ISeqSettings
    {
        var seqSettings = services.GetSettings<TSettings, SeqSettings?>(x => x.Seq);

        return services.AddCommonLoggingCore(seqSettings);
    }

    /// <summary>
    /// Configures Serilog without Seq: the console sink plus the OTLP sink when an endpoint is configured.
    /// </summary>
    public static IServiceCollection AddCommonLogging(this IServiceCollection services)
    {
        return services.AddCommonLoggingCore(seqSettings: null);
    }

    private static IServiceCollection AddCommonLoggingCore(this IServiceCollection services, SeqSettings? seqSettings)
    {
        var otlp = OtlpConfiguration.FromServices(services);
        var logsTarget = otlp.ResolveExport(OtlpSignal.Logs);
        var isSeqEnabled = !string.IsNullOrWhiteSpace(seqSettings?.Url);

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", $"{ApplicationName()}")
            .Enrich.FromLogContext()
            .WriteTo.Console();

        if (isSeqEnabled)
        {
            loggerConfiguration.WriteTo.Seq(seqSettings!.Url, apiKey: seqSettings.ApiKey);
        }

        if (logsTarget is not null)
        {
            var resourceAttributes = otlp.ResolveResourceAttributes();

            // The environment is already resolved by OtlpConfiguration with the documented precedence,
            // so the sink must not apply its own OTEL_* overrides on top of it.
            loggerConfiguration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = null;
                options.LogsEndpoint = logsTarget.Endpoint.ToString();
                options.Protocol = logsTarget.Protocol == OtlpTransport.Grpc
                    ? OtlpProtocol.Grpc
                    : OtlpProtocol.HttpProtobuf;
                options.Headers = logsTarget.ParseHeaders();
                options.ResourceAttributes = new Dictionary<string, object>(resourceAttributes);
            }, ignoreEnvironment: true);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        Log.Information("{ApplicationName} - Application starting up", ApplicationName());

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });

        if (isSeqEnabled)
        {
            services.AddCommonHealthCheck<SeqHealthCheck>();
        }

        return services;
    }
}
