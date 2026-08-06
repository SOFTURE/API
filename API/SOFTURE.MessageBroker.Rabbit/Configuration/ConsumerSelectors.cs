using MassTransit;

namespace SOFTURE.MessageBroker.Rabbit.Configuration;

public static class ConsumerSelectors
{
    public static Func<Type, bool> ForMessages<TMarker>() where TMarker : class
    {
        return consumerType => GetMessageTypes(consumerType).Any(messageType => typeof(TMarker).IsAssignableFrom(messageType));
    }

    public static Func<Type, bool> ForMessage<TMessage>() where TMessage : class
    {
        return consumerType => GetMessageTypes(consumerType).Any(messageType => messageType == typeof(TMessage));
    }

    public static Func<Type, bool> ForConsumers(params Type[] consumerTypes)
    {
        var selected = new HashSet<Type>(consumerTypes);

        return consumerType => selected.Contains(consumerType);
    }

    public static Func<Type, bool> ForNamespace(string namespacePrefix)
    {
        return consumerType => consumerType.Namespace?.StartsWith(namespacePrefix, StringComparison.Ordinal) == true;
    }

    public static Func<Type, bool> Any(params Func<Type, bool>[] selectors)
    {
        return consumerType => selectors.Any(selector => selector(consumerType));
    }

    internal static IEnumerable<Type> GetMessageTypes(Type consumerType)
    {
        return consumerType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
            .Select(i => i.GetGenericArguments()[0]);
    }
}
