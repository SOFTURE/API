namespace SOFTURE.MessageBroker.Rabbit.Configuration;

public sealed class RetrySettings
{
    private readonly List<Type> _ignoredExceptions = new();

    public int Count { get; set; } = 5;

    public bool IsExponential { get; set; } = true;

    public TimeSpan MinInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxInterval { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan IntervalDelta { get; set; } = TimeSpan.FromSeconds(1);

    public IReadOnlyCollection<Type> IgnoredExceptions => _ignoredExceptions;

    public RetrySettings Ignore<TException>() where TException : Exception
    {
        _ignoredExceptions.Add(typeof(TException));

        return this;
    }

    public RetrySettings Ignore(params Type[] exceptionTypes)
    {
        foreach (var exceptionType in exceptionTypes)
        {
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
                throw new ArgumentException($"Type '{exceptionType.FullName}' is not an exception type.", nameof(exceptionTypes));

            _ignoredExceptions.Add(exceptionType);
        }

        return this;
    }
}
