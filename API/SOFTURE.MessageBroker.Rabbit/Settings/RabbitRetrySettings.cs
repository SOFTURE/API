namespace SOFTURE.MessageBroker.Rabbit.Settings;

public sealed class RabbitRetrySettings
{
    public int Count { get; init; } = 5;

    public bool IsExponential { get; init; } = true;

    public int MinIntervalSeconds { get; init; } = 1;

    public int MaxIntervalSeconds { get; init; } = 120;

    public int IntervalDeltaSeconds { get; init; } = 1;
}
