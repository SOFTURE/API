namespace SOFTURE.Common.Observability.Settings;

public sealed class ObservabilitySettings
{
#if NET8_0
    public required string Url { get; init; }
#endif

#if NET6_0
    public string Url { get; init; } = null!;
#endif
}