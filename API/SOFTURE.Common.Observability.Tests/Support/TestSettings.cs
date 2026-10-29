using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Tests.Support;

internal sealed class TestSettings : IOpenTelemetrySettings, ISeqSettings
{
    public required OpenTelemetrySettings OpenTelemetry { get; init; }

    public required SeqSettings Seq { get; init; }
}
