using OpenTelemetry.Exporter;
using SOFTURE.Common.Observability.Extensions;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Telemetry;

internal static class OtlpExporterOptionsExtensions
{
    public static void UseOpenTelemetrySettings(this OtlpExporterOptions options, OpenTelemetrySettings settings, OtlpSignal signal)
    {
        options.Endpoint = settings.GetSignalEndpoint(signal);
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Headers = settings.Headers;
    }
}
