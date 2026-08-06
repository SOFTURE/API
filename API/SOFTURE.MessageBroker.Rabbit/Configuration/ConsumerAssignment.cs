namespace SOFTURE.MessageBroker.Rabbit.Configuration;

internal sealed class ConsumerAssignment
{
    private ConsumerAssignment(IReadOnlyList<Type> defaultConsumers, IReadOnlyDictionary<string, List<Type>> groups)
    {
        Default = defaultConsumers;
        Groups = groups;
    }

    public IReadOnlyList<Type> Default { get; }

    public IReadOnlyDictionary<string, List<Type>> Groups { get; }

    public static ConsumerAssignment Create(IReadOnlyCollection<Type> consumerTypes, RabbitConsumersOptions options)
    {
        var groups = options.Groups.ToDictionary(group => group.Name, _ => new List<Type>());
        var defaultConsumers = new List<Type>();

        foreach (var consumerType in consumerTypes)
        {
            var group = options.Groups.FirstOrDefault(candidate => candidate.Selector(consumerType));

            if (group is null)
                defaultConsumers.Add(consumerType);
            else
                groups[group.Name].Add(consumerType);
        }

        return new ConsumerAssignment(defaultConsumers, groups);
    }

    public static string GetEndpointName(string queueName, string groupName, string separator) => $"{queueName}{separator}{groupName}";
}
