using SOFTURE.Common.Observability.Models;

namespace SOFTURE.Common.Observability.Settings;

public sealed class MetricsSettings
{
    public MetricsProfile Profile { get; init; } = MetricsProfile.Minimal;
    public bool AspNetCoreEnabled { get; init; } = true;
    public bool HttpClientEnabled { get; init; } = true;
    public bool RuntimeEnabled { get; init; } = true;
    public bool GroupStatusCodes { get; init; }
    public bool ExcludeUnmatchedRoutes { get; init; }
    public string[] DroppedInstruments { get; init; } = [];
    public string[] DroppedRequestDurationTags { get; init; } = [];
    public string[] ExcludedRoutes { get; init; } = [];
    public string[] ExcludedHttpClientHosts { get; init; } = [];
    public double[] RequestDurationBoundaries { get; init; } = [];
}
