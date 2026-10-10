namespace SOFTURE.Common.Observability.Settings;

public interface IOpenTelemetrySettings
{
    OpenTelemetrySettings OpenTelemetry { get; init; }
}
