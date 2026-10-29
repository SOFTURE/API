namespace SOFTURE.Common.Observability.Extensions;

internal static class TagExtensions
{
    public static object? FindTag(this ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == key)
            {
                return tag.Value;
            }
        }

        return null;
    }
}
