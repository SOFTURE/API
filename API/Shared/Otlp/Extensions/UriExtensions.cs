using SOFTURE.Common.Otlp.Models;

namespace SOFTURE.Common.Otlp.Extensions;

internal static class UriExtensions
{
    public static bool IsSameOrigin(this Uri endpoint, Uri? requestUri) =>
        requestUri is not null
        && requestUri.IsAbsoluteUri
        && string.Equals(requestUri.Scheme, endpoint.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(requestUri.Host, endpoint.Host, StringComparison.OrdinalIgnoreCase)
        && requestUri.Port == endpoint.Port;

    public static Uri WithSignalPath(this Uri endpoint, OtlpTransport protocol, OtlpSignal signal)
    {
        if (protocol == OtlpTransport.Grpc)
        {
            return endpoint;
        }

        var signalPath = "/v1/" + signal.ToString().ToLowerInvariant();
        var basePath = endpoint.AbsolutePath.TrimEnd('/');

        if (basePath.EndsWith(signalPath, StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        return new UriBuilder(endpoint) { Path = basePath + signalPath }.Uri;
    }
}
