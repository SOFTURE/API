namespace SOFTURE.Common.Observability.Export;

internal static class ExportHttpMessageHandler
{
    public static HttpMessageHandler Create()
    {
#if NET8_0_OR_GREATER
        return new SocketsHttpHandler
        {
            MeterFactory = UnobservedMeterFactory.Instance,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };
#else
        return new HttpClientHandler();
#endif
    }
}
