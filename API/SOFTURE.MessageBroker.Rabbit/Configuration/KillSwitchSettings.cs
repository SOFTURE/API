namespace SOFTURE.MessageBroker.Rabbit.Configuration;

public sealed class KillSwitchSettings
{
    public int ActivationThreshold { get; set; } = 10;

    public double TripThreshold { get; set; } = 0.5;

    public TimeSpan TrackingPeriod { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan RestartTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
