using SOFTURE.Common.Otlp;
using SOFTURE.Common.Otlp.Extensions;
using SOFTURE.Common.Otlp.Models;

namespace SOFTURE.Common.Observability.Exporters;

internal sealed class ObservabilityExportPlan
{
    private readonly IReadOnlyList<Uri> _exportEndpoints;

    private ObservabilityExportPlan(SignalExport? traces, SignalExport? metrics, IReadOnlyList<Uri> exportEndpoints)
    {
        Traces = traces;
        Metrics = metrics;
        _exportEndpoints = exportEndpoints;
    }

    public SignalExport? Traces { get; }
    public SignalExport? Metrics { get; }

    public static ObservabilityExportPlan Create(OtlpConfiguration otlp, string? legacyUrl)
    {
        var legacyEndpoint = string.IsNullOrWhiteSpace(legacyUrl) ? null : new Uri(legacyUrl);

        var traces = SignalExport.From(otlp.ResolveExport(OtlpSignal.Traces), legacyEndpoint);
        var metrics = SignalExport.From(otlp.ResolveExport(OtlpSignal.Metrics), legacyEndpoint);
        var logs = otlp.ResolveExport(OtlpSignal.Logs);

        var exportEndpoints = new[] { traces?.Endpoint, metrics?.Endpoint, logs?.Endpoint }
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        return new ObservabilityExportPlan(traces, metrics, exportEndpoints);
    }

    public bool IsExportRequest(Uri? requestUri) => _exportEndpoints.Any(endpoint => endpoint.IsSameOrigin(requestUri));
}
