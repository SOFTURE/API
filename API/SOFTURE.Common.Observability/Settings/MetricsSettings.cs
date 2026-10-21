using SOFTURE.Common.Observability.Models;

namespace SOFTURE.Common.Observability.Settings;

public sealed class MetricsSettings
{
    public MetricsProfile Profile { get; init; } = MetricsProfile.Minimal;

    public string[] DroppedInstruments { get; init; } = [];
}
