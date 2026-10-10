namespace SOFTURE.Common.Otlp.Extensions;

public static class StringExtensions
{
    public static IDictionary<string, string> ToKeyValuePairs(this string value)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var member in value.Split(','))
        {
            if (string.IsNullOrWhiteSpace(member))
            {
                continue;
            }

            var separator = member.IndexOf('=');

            if (separator <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid entry '{member.Trim()}' in OpenTelemetry:Headers: expected the 'key=value,key2=value2' format.");
            }

            result[member.Substring(0, separator).Trim()] = Uri.UnescapeDataString(member.Substring(separator + 1).Trim());
        }

        return result;
    }

    public static string WithoutBuildMetadata(this string version)
    {
        var metadataSeparator = version.IndexOf('+');

        return metadataSeparator > 0 ? version.Substring(0, metadataSeparator) : version;
    }
}
