namespace SOFTURE.Common.Otlp.Extensions;

internal static class DictionaryExtensions
{
    public static void SetIfPresent(this IDictionary<string, object> attributes, string key, string? value)
    {
        if (value is not null)
        {
            attributes[key] = value;
        }
    }

    public static void SetIfMissing(this IDictionary<string, object> attributes, string key, string? value)
    {
        if (value is not null && !attributes.ContainsKey(key))
        {
            attributes[key] = value;
        }
    }

    public static string? GetNonBlankString(this IDictionary<string, object> attributes, string key) =>
        attributes.TryGetValue(key, out var value) ? (value as string).NullIfBlank() : null;
}
