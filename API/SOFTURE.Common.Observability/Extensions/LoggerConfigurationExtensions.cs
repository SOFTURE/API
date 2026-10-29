using Serilog;
using Serilog.Sinks.OpenTelemetry;
using SOFTURE.Common.Observability.Export;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Extensions;

internal static class LoggerConfigurationExtensions
{
    public static LoggerConfiguration WriteToOpenTelemetry(this LoggerConfiguration configuration, OpenTelemetrySettings settings)
    {
        return configuration.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = null;
            options.LogsEndpoint = settings.GetSignalEndpoint(OtlpSignal.Logs).ToString();
            options.Protocol = OtlpProtocol.HttpProtobuf;
            options.Headers = settings.GetHeaders();
            options.ResourceAttributes = new Dictionary<string, object>(settings.GetResourceAttributes());
            options.HttpMessageHandler = ExportHttpMessageHandler.Create();
        }, ignoreEnvironment: true);
    }

    public static LoggerConfiguration WriteToSeq(this LoggerConfiguration configuration, SeqSettings settings)
    {
        return configuration.WriteTo.Seq(settings.Url, apiKey: settings.ApiKey, messageHandler: ExportHttpMessageHandler.Create());
    }
}
