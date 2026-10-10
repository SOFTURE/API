using OpenTelemetry.Exporter;
using SOFTURE.Common.Otlp.Extensions;
using SOFTURE.Common.Otlp.Models;
using SOFTURE.Common.Otlp.Settings;

namespace SOFTURE.Common.Observability.Extensions;

internal static class OtlpExporterOptionsExtensions
{
    public static void UseOpenTelemetrySettings(this OtlpExporterOptions options, OpenTelemetrySettings settings, OtlpSignal signal)
    {
        options.Endpoint = settings.GetSignalEndpoint(signal);
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Headers = settings.Headers;
    }
}
