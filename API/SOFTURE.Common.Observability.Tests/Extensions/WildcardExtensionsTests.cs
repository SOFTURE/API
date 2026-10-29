using Xunit;
using SOFTURE.Common.Observability.Extensions;

namespace SOFTURE.Common.Observability.Tests.Extensions;

public sealed class WildcardExtensionsTests
{
    [Theory]
    [InlineData("kestrel.connection.duration", "kestrel.*", true)]
    [InlineData("r11.c.lencr.org", "*.lencr.org", true)]
    [InlineData("internal/ping/now", "*ping*", true)]
    [InlineData("HC", "hc", true)]
    [InlineData("anything", "*", true)]
    [InlineData("hc/details", "hc", false)]
    [InlineData("lencr.org", "*.lencr.org", false)]
    [InlineData("api.example.com", "crl*", false)]
    public void Matches_exact_prefix_suffix_and_infix_patterns_ignoring_case(string value, string pattern, bool expected)
    {
        Assert.Equal(expected, value.MatchesWildcard(pattern));
    }
}
