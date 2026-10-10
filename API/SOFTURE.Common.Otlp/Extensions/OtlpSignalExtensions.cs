using SOFTURE.Common.Otlp.Models;

namespace SOFTURE.Common.Otlp.Extensions;

public static class OtlpSignalExtensions
{
    public static string ToHttpPath(this OtlpSignal signal) => "/v1/" + signal.ToString().ToLowerInvariant();
}
