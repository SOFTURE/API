namespace SOFTURE.Common.Observability.Abstractions;

internal interface IRequestFilter
{
    bool IsExcluded(ReadOnlySpan<KeyValuePair<string, object?>> tags);
}
