namespace SOFTURE.MessageBroker.Rabbit.Settings;

public sealed class RabbitSettings
{
#if NET8_0
    public required string Name { get; init; }
    public required string Url { get; init; }
#endif

#if NET6_0
    public string Name { get; init; } = null!;
    public string Url { get; init; } = null!;
#endif
}