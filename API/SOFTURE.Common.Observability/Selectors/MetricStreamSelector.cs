using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using SOFTURE.Common.Observability.Extensions;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Selectors;

internal sealed class MetricStreamSelector
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

    private static readonly double[] MinimalRequestDurationBoundaries = [0.05, 0.25, 1, 5];

    private static readonly double[] FullRequestDurationBoundaries =
        [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10];

    private readonly MetricsSettings _settings;
    private readonly double[] _requestDurationBoundaries;

    public MetricStreamSelector(MetricsSettings settings)
    {
        _settings = settings;
        _requestDurationBoundaries = GetRequestDurationBoundaries(settings);
    }

    public MetricStreamConfiguration? Select(Instrument instrument)
    {
        if (RequestDurationInstrument.IsAnySource(instrument))
        {
            return MetricStreamConfiguration.Drop;
        }

        if (instrument.Name.MatchesAnyWildcard(_settings.DroppedInstruments))
        {
            return MetricStreamConfiguration.Drop;
        }

        if (RequestDurationInstrument.IsAnyRelay(instrument))
        {
            return new ExplicitBucketHistogramConfiguration { Boundaries = _requestDurationBoundaries };
        }

        if (_settings.Profile == MetricsProfile.Full)
        {
            return null;
        }

        if (instrument.Name.MatchesAnyWildcard(LowValueInstruments))
        {
            return MetricStreamConfiguration.Drop;
        }

        return null;
    }

    private static double[] GetRequestDurationBoundaries(MetricsSettings settings)
    {
        if (settings.RequestDurationBoundaries.Length == 0 && settings.Profile == MetricsProfile.Full)
        {
            return FullRequestDurationBoundaries;
        }

        if (settings.RequestDurationBoundaries.Length == 0)
        {
            return MinimalRequestDurationBoundaries;
        }

        if (!IsStrictlyAscending(settings.RequestDurationBoundaries))
        {
            throw new InvalidOperationException(
                $"Invalid OpenTelemetry:Metrics:RequestDurationBoundaries '{string.Join(", ", settings.RequestDurationBoundaries)}': expected finite values in ascending order.");
        }

        return settings.RequestDurationBoundaries;
    }

    private static bool IsStrictlyAscending(double[] boundaries)
    {
        for (var index = 0; index < boundaries.Length; index++)
        {
            if (!double.IsFinite(boundaries[index]))
            {
                return false;
            }

            if (index > 0 && boundaries[index] <= boundaries[index - 1])
            {
                return false;
            }
        }

        return true;
    }
}
