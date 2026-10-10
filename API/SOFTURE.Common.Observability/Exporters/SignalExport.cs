using OpenTelemetry.Exporter;
using SOFTURE.Common.Otlp.Models;

namespace SOFTURE.Common.Observability.Exporters;

internal sealed class SignalExport
{
    private readonly OtlpExportTarget? _target;

    private SignalExport(OtlpExportTarget? target, Uri endpoint)
    {
        _target = target;
        Endpoint = endpoint;
    }

    public Uri Endpoint { get; }

    public static SignalExport? From(OtlpExportTarget? target, Uri? legacyEndpoint)
    {
        if (target is not null)
        {
            return new SignalExport(target, target.Endpoint);
        }

        return legacyEndpoint is null ? null : new SignalExport(null, legacyEndpoint);
    }

    public void ApplyTo(OtlpExporterOptions options)
    {
        options.Endpoint = Endpoint;

        if (_target is null)
        {
            return;
        }

        options.Protocol = _target.Protocol == OtlpTransport.Grpc
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;

        if (_target.Headers is not null)
        {
            options.Headers = _target.Headers;
        }
    }
}
