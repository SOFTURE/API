using SOFTURE.Contract.Common.Messaging;

namespace SOFTURE.MessageBroker.Rabbit.Configuration;

public sealed class RabbitConsumersOptions
{
    private readonly List<ConsumerGroupOptions> _groups = new();

    public ConsumerEndpointOptions Default { get; } = new();

    public string GroupSeparator { get; set; } = ".";

    public IReadOnlyList<ConsumerGroupOptions> Groups => _groups;

    public RabbitConsumersOptions AddGroup(string name, Func<Type, bool> selector, Action<ConsumerGroupOptions>? configure = null)
    {
        if (_groups.Any(group => string.Equals(group.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Consumer group '{name}' is already registered.");

        var group = new ConsumerGroupOptions(name, selector);

        configure?.Invoke(group);

        _groups.Add(group);

        return this;
    }

    public RabbitConsumersOptions AddBulkGroup(string name = "bulk", Action<ConsumerGroupOptions>? configure = null)
    {
        return AddGroup(name, ConsumerSelectors.ForMessages<IBulkMessage>(), configure);
    }

    internal void Validate()
    {
        if (string.IsNullOrEmpty(GroupSeparator) && _groups.Count > 0)
            throw new InvalidOperationException("GroupSeparator cannot be empty when consumer groups are registered — group endpoints would collide with the default endpoint name.");

        Validate(Default, "default");

        foreach (var group in _groups)
            Validate(group, group.Name);
    }

    private static void Validate(ConsumerEndpointOptions options, string endpointName)
    {
        if (options.PrefetchCount < 1)
            throw new InvalidOperationException($"PrefetchCount for endpoint '{endpointName}' must be greater than zero.");

        if (options.ConcurrentMessageLimit is { } limit)
        {
            if (limit < 1)
                throw new InvalidOperationException($"ConcurrentMessageLimit for endpoint '{endpointName}' must be greater than zero.");

            if (limit > options.PrefetchCount)
                throw new InvalidOperationException(
                    $"ConcurrentMessageLimit ({limit}) for endpoint '{endpointName}' exceeds PrefetchCount ({options.PrefetchCount}); the broker would never deliver enough messages to saturate the consumers.");
        }

        if (options.RateLimit is { } rateLimit && rateLimit.Limit < 1)
            throw new InvalidOperationException($"RateLimit.Limit for endpoint '{endpointName}' must be greater than zero.");

        if (options.KillSwitch is { } killSwitch && killSwitch.TripThreshold is <= 0 or > 1)
            throw new InvalidOperationException($"KillSwitch.TripThreshold for endpoint '{endpointName}' must be a fraction in the (0, 1] range.");
    }
}
