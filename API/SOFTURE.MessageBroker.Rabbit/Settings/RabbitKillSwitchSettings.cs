namespace SOFTURE.MessageBroker.Rabbit.Settings;

public sealed class RabbitKillSwitchSettings
{
    public bool Enabled { get; init; }

    public int ActivationThreshold { get; init; } = 10;

    public double TripThreshold { get; init; } = 0.5;

    public int TrackingPeriodSeconds { get; init; } = 600;

    public int RestartSeconds { get; init; } = 300;
}
