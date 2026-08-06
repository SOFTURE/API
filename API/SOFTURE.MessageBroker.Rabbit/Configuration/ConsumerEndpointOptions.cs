using MassTransit;

namespace SOFTURE.MessageBroker.Rabbit.Configuration;

public class ConsumerEndpointOptions
{
    public int PrefetchCount { get; set; } = 32;

    public int? ConcurrentMessageLimit { get; set; } = 16;

    public TimeSpan? ConsumeTimeout { get; set; }

    public RetrySettings? Retry { get; set; } = new();

    public Action<IRetryConfigurator>? ConfigureRetry { get; set; }

    public KillSwitchSettings? KillSwitch { get; set; }

    public RateLimitSettings? RateLimit { get; set; }

    public ConsumerEndpointOptions WithRetry(Action<RetrySettings> configure)
    {
        Retry ??= new RetrySettings();

        configure(Retry);

        return this;
    }

    public ConsumerEndpointOptions WithKillSwitch(Action<KillSwitchSettings>? configure = null)
    {
        KillSwitch ??= new KillSwitchSettings();

        configure?.Invoke(KillSwitch);

        return this;
    }

    public ConsumerEndpointOptions WithRateLimit(int limit, TimeSpan interval)
    {
        RateLimit = new RateLimitSettings { Limit = limit, Interval = interval };

        return this;
    }

    public ConsumerEndpointOptions WithoutRetry()
    {
        Retry = null;
        ConfigureRetry = null;

        return this;
    }
}
