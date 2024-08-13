using SOFTURE.Common.Correlation.ValueObjects;

namespace SOFTURE.Common.Correlation.Generators;

public static class CorrelationGenerator
{
    public static CorrelationId Generate() => new(Guid.NewGuid());
}