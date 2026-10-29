namespace SOFTURE.Common.Observability.Models;

internal static class RequestTags
{
    public const string Route = "http.route";
    public const string StatusCode = "http.response.status_code";
    public const string StatusClass = "http.response.status_class";
    public const string ErrorType = "error.type";
    public const string ServerAddress = "server.address";
    public const string ServerPort = "server.port";

    private static readonly string[] StatusClasses = ["1xx", "2xx", "3xx", "4xx", "5xx"];

    public static string? ToStatusClass(object? statusCode)
    {
        var code = statusCode switch
        {
            int value => value,
            long value => (int)value,
            string value when int.TryParse(value, out var parsed) => parsed,
            _ => 0
        };

        var index = code / 100 - 1;

        return index >= 0 && index < StatusClasses.Length ? StatusClasses[index] : null;
    }
}
