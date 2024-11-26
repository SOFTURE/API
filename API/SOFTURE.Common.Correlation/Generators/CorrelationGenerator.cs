using SOFTURE.Common.Correlation.ValueObjects;

namespace SOFTURE.Common.Correlation.Generators;

public static class CorrelationGenerator
{
    public static CorrelationId Generate()
    {
# if NET9_0
        return new CorrelationId(Guid.CreateVersion7());
# endif
        
#if NET8_0
        return new CorrelationId(Guid.NewGuid());
# endif
        
#if NET6_0
        return new CorrelationId(Guid.NewGuid());
# endif
    }
}