using Serilog;
using Serilog.Sinks.OpenTelemetry;
using SOFTURE.Common.Logging.Settings;
using SOFTURE.Common.Otlp;
using SOFTURE.Common.Otlp.Models;

namespace SOFTURE.Common.Logging.Extensions;

internal static class LoggerConfigurationExtensions
{
    public static LoggerConfiguration WriteToSeqIfConfigured(this LoggerConfiguration configuration, SeqSettings? seqSettings)
    {
        if (seqSettings.IsConfigured())
        {
            configuration.WriteTo.Seq(seqSettings!.Url, apiKey: seqSettings.ApiKey);
        }

        return configuration;
    }

    public static LoggerConfiguration WriteToOtlpIfConfigured(this LoggerConfiguration configuration, OtlpConfiguration otlp)
    {
        if (otlp.ResolveExport(OtlpSignal.Logs) is not { } logsTarget)
        {
            return configuration;
        }

        var resourceAttributes = otlp.ResolveResourceAttributes();

        return configuration.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = null;
            options.LogsEndpoint = logsTarget.Endpoint.ToString();
            options.Protocol = logsTarget.Protocol.ToSinkProtocol();
            options.Headers = logsTarget.ParseHeaders();
            options.ResourceAttributes = new Dictionary<string, object>(resourceAttributes);
        }, ignoreEnvironment: true);
    }

    public static bool IsConfigured(this SeqSettings? seqSettings) => !string.IsNullOrWhiteSpace(seqSettings?.Url);

    private static OtlpProtocol ToSinkProtocol(this OtlpTransport transport) =>
        transport == OtlpTransport.Grpc ? OtlpProtocol.Grpc : OtlpProtocol.HttpProtobuf;
}
