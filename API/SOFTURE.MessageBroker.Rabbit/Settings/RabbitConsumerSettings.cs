namespace SOFTURE.MessageBroker.Rabbit.Settings;

public sealed class RabbitConsumerSettings
{
    public int PrefetchCount { get; init; } = 32;

    public int ConcurrentMessageLimit { get; init; } = 16;

    public RabbitRetrySettings Retry { get; init; } = new();

    public RabbitKillSwitchSettings KillSwitch { get; init; } = new();

    public IDictionary<string, RabbitConsumerSettings> Groups { get; init; } = new Dictionary<string, RabbitConsumerSettings>(StringComparer.OrdinalIgnoreCase);

    public RabbitConsumerSettings Group(string name)
    {
        return Groups.TryGetValue(name, out var group)
            ? group
            : throw new InvalidOperationException($"Missing configuration for consumer group '{name}' in section 'Rabbit:Consumers:Groups'.");
    }
}
