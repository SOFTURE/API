using SOFTURE.Common.Observability.Models;

namespace SOFTURE.Common.Observability.Extensions;

public static class OtlpSignalExtensions
{
    public static string ToHttpPath(this OtlpSignal signal) => "/v1/" + signal.ToString().ToLowerInvariant();
}
