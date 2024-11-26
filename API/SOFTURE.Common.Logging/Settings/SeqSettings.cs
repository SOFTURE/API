namespace SOFTURE.Common.Logging.Settings;

public sealed class SeqSettings
{
#if NET8_0_OR_GREATER
    public required string Url { get; init; }
    public required string ApiKey { get; init; }
#endif

#if NET6_0
    public string Url { get; init; } = null!;
    public string ApiKey { get; init; } = null!;
#endif
}