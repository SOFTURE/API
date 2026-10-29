using SOFTURE.Common.Observability.Abstractions;
using SOFTURE.Common.Observability.Export;
using SOFTURE.Common.Observability.Extensions;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Filters;

internal sealed class HttpClientRequestFilter(MetricsSettings settings, ExportOrigins exportOrigins) : IRequestFilter
{
    private static readonly string[] CertificateRevocationHosts = ["crl*", "ocsp*", "*.lencr.org", "*.pki.goog"];

    private readonly string[] _excludedHosts = GetExcludedHosts(settings);

    public bool IsExcluded(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.FindTag(RequestTags.ServerAddress) is not string host)
        {
            return false;
        }

        var port = tags.FindTag(RequestTags.ServerPort) as int?;

        return exportOrigins.Contains(host, port) || host.MatchesAnyWildcard(_excludedHosts);
    }

    private static string[] GetExcludedHosts(MetricsSettings settings)
    {
        if (settings.Profile == MetricsProfile.Minimal)
        {
            return CertificateRevocationHosts.Concat(settings.ExcludedHttpClientHosts).ToArray();
        }

        return settings.ExcludedHttpClientHosts;
    }
}
