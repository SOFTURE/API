namespace SOFTURE.Common.Observability.Extensions;

internal static class WildcardExtensions
{
    private const char Wildcard = '*';

    public static bool MatchesAnyWildcard(this string value, IEnumerable<string> patterns)
    {
        return patterns.Any(value.MatchesWildcard);
    }

    public static bool MatchesWildcard(this string value, string pattern)
    {
        var hasLeadingWildcard = pattern.StartsWith(Wildcard);
        var hasTrailingWildcard = pattern.Length > 1 && pattern.EndsWith(Wildcard);
        var literal = pattern.Trim(Wildcard);

        if (hasLeadingWildcard && hasTrailingWildcard)
        {
            return value.IndexOf(literal, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        if (hasLeadingWildcard)
        {
            return value.EndsWith(literal, StringComparison.OrdinalIgnoreCase);
        }

        if (hasTrailingWildcard)
        {
            return value.StartsWith(literal, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
