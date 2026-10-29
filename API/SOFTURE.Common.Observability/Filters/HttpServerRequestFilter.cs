using SOFTURE.Common.Observability.Abstractions;
using SOFTURE.Common.Observability.Extensions;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Filters;

internal sealed class HttpServerRequestFilter(MetricsSettings settings) : IRequestFilter
{
    private readonly string[] _excludedRoutes = settings.ExcludedRoutes.Select(TrimLeadingSlash).ToArray();

    public bool IsExcluded(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.FindTag(RequestTags.Route) is not string { Length: > 0 } route)
        {
            return settings.ExcludeUnmatchedRoutes;
        }

        return TrimLeadingSlash(route).MatchesAnyWildcard(_excludedRoutes);
    }

    private static string TrimLeadingSlash(string route)
    {
        return route.TrimStart('/');
    }
}
