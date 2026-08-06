namespace SOFTURE.MessageBroker.Rabbit.Configuration;

public sealed class RateLimitSettings
{
    public int Limit { get; set; } = 100;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
}
