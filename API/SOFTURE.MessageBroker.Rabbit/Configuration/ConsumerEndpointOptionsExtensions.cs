using SOFTURE.MessageBroker.Rabbit.Settings;

namespace SOFTURE.MessageBroker.Rabbit.Configuration;

public static class ConsumerEndpointOptionsExtensions
{
    public static TOptions ApplyFrom<TOptions>(this TOptions options, RabbitConsumerSettings settings)
        where TOptions : ConsumerEndpointOptions
    {
        options.PrefetchCount = settings.PrefetchCount;
        options.ConcurrentMessageLimit = settings.ConcurrentMessageLimit;

        options.WithRetry(retry =>
        {
            retry.Count = settings.Retry.Count;
            retry.IsExponential = settings.Retry.IsExponential;
            retry.MinInterval = TimeSpan.FromSeconds(settings.Retry.MinIntervalSeconds);
            retry.MaxInterval = TimeSpan.FromSeconds(settings.Retry.MaxIntervalSeconds);
            retry.IntervalDelta = TimeSpan.FromSeconds(settings.Retry.IntervalDeltaSeconds);
        });

        if (!settings.KillSwitch.Enabled)
            return options;

        options.WithKillSwitch(killSwitch =>
        {
            killSwitch.ActivationThreshold = settings.KillSwitch.ActivationThreshold;
            killSwitch.TripThreshold = settings.KillSwitch.TripThreshold;
            killSwitch.TrackingPeriod = TimeSpan.FromSeconds(settings.KillSwitch.TrackingPeriodSeconds);
            killSwitch.RestartTimeout = TimeSpan.FromSeconds(settings.KillSwitch.RestartSeconds);
        });

        return options;
    }
}
