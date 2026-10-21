using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Telemetry;

internal static class MetricStreamSelector
{
    private static readonly string[] LowValueInstruments =
    [
        "dns.lookup.duration",
        "http.client.connection.duration",
        "http.client.request.time_in_queue",
        "http.client.open_connections",
        "http.client.active_requests",
        "aspnetcore.authentication.authenticate.duration",
        "aspnetcore.routing.match_attempts",
        "kestrel.connection.duration",
        "aspnetcore.rate_limiting.request_lease.duration",
        "aspnetcore.memory_pool.*"
    ];

    private static readonly string[] RequestDurationInstruments =
    [
        "http.server.request.duration",
        "http.client.request.duration"
    ];

    private static readonly double[] RequestDurationBoundaries = [0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10];

    public static MetricStreamConfiguration? Select(Instrument instrument, MetricsSettings settings)
    {
        if (MatchesAny(instrument.Name, settings.DroppedInstruments))
        {
            return MetricStreamConfiguration.Drop;
        }

        if (settings.Profile == MetricsProfile.Full)
        {
            return null;
        }

        if (MatchesAny(instrument.Name, LowValueInstruments))
        {
            return MetricStreamConfiguration.Drop;
        }

        if (MatchesAny(instrument.Name, RequestDurationInstruments))
        {
            return new ExplicitBucketHistogramConfiguration { Boundaries = RequestDurationBoundaries };
        }

        return null;
    }

    private static bool MatchesAny(string instrumentName, IEnumerable<string> patterns)
    {
        return patterns.Any(pattern => Matches(instrumentName, pattern));
    }

    private static bool Matches(string instrumentName, string pattern)
    {
        if (pattern.EndsWith("*", StringComparison.Ordinal))
        {
            return instrumentName.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(instrumentName, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
