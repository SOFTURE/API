using SOFTURE.Common.Correlation.ValueObjects;

namespace SOFTURE.Common.Correlation.Providers;

public interface ICorrelationProvider
{
    CorrelationId? Get();
    void Set(CorrelationId correlationId);
}

public sealed class CorrelationProvider : ICorrelationProvider
{
    private CorrelationId? CorrelationId { get; set; }
    
    public CorrelationId? Get() => CorrelationId;

    public void Set(CorrelationId correlationId) => CorrelationId = correlationId;
}