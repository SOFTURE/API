using SOFTURE.Common.Otlp.Models;

namespace SOFTURE.Common.Otlp.Extensions;

internal static class StringExtensions
{
    public static string? NullIfBlank(this string? value) => string.IsNullOrWhiteSpace(value) ? null : value!.Trim();

    public static IDictionary<string, string> ToKeyValuePairs(this string? value, string source)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        foreach (var member in value!.Split(','))
        {
            if (string.IsNullOrWhiteSpace(member))
            {
                continue;
            }

            var separator = member.IndexOf('=');
            var key = separator > 0 ? member.Substring(0, separator).Trim() : string.Empty;

            if (key.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Invalid entry '{member.Trim()}' in {source}: expected the 'key=value,key2=value2' format.");
            }

            result[key] = Uri.UnescapeDataString(member.Substring(separator + 1).Trim());
        }

        return result;
    }

    public static string? EnsureValidKeyValueList(this string? value, string source)
    {
        value.ToKeyValuePairs(source);

        return value;
    }

    public static Uri ToAbsoluteUri(this string value, string source) =>
        Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidOperationException($"Invalid OTLP endpoint '{value}' in {source}: expected an absolute URL.");

    public static OtlpTransport? ToOtlpTransport(this string? value, string source)
    {
        var normalized = value.NullIfBlank()?.Replace("/", string.Empty).Replace("-", string.Empty).ToLowerInvariant();

        return normalized switch
        {
            null => null,
            "grpc" => OtlpTransport.Grpc,
            "httpprotobuf" => OtlpTransport.HttpProtobuf,
            _ => throw new InvalidOperationException(
                $"Unsupported OTLP protocol '{value}' in {source}: use 'HttpProtobuf' (http/protobuf) or 'Grpc'.")
        };
    }

    public static bool? ToBoolean(this string? value, string source)
    {
        if (value.NullIfBlank() is not { } text)
        {
            return null;
        }

        return bool.TryParse(text, out var result)
            ? result
            : throw new InvalidOperationException($"Invalid boolean '{value}' in {source}.");
    }

    public static string WithoutBuildMetadata(this string version)
    {
        var metadataSeparator = version.IndexOf('+');

        return metadataSeparator > 0 ? version.Substring(0, metadataSeparator) : version;
    }
}
