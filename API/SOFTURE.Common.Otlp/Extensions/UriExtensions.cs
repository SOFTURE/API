namespace SOFTURE.Common.Otlp.Extensions;

public static class UriExtensions
{
    public static bool IsSameOrigin(this Uri endpoint, Uri? requestUri) =>
        requestUri is not null
        && requestUri.IsAbsoluteUri
        && string.Equals(requestUri.Scheme, endpoint.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(requestUri.Host, endpoint.Host, StringComparison.OrdinalIgnoreCase)
        && requestUri.Port == endpoint.Port;

    public static Uri WithPathSuffix(this Uri endpoint, string suffix)
    {
        var basePath = endpoint.AbsolutePath.TrimEnd('/');

        if (basePath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        return new UriBuilder(endpoint) { Path = basePath + suffix }.Uri;
    }
}
