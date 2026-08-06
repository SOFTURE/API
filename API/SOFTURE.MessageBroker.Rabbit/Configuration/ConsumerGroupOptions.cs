namespace SOFTURE.MessageBroker.Rabbit.Configuration;

public sealed class ConsumerGroupOptions : ConsumerEndpointOptions
{
    internal ConsumerGroupOptions(string name, Func<Type, bool> selector)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Consumer group name cannot be empty.", nameof(name));

        Name = name;
        Selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    public string Name { get; }

    internal Func<Type, bool> Selector { get; }
}
