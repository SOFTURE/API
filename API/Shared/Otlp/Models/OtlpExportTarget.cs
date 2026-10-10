using SOFTURE.Common.Otlp.Extensions;

namespace SOFTURE.Common.Otlp.Models;

internal sealed record OtlpExportTarget(Uri Endpoint, OtlpTransport Protocol, string? Headers)
{
    public IDictionary<string, string> ParseHeaders() => Headers.ToKeyValuePairs("Headers");
}
