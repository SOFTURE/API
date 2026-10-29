using System.Diagnostics;
using System.Diagnostics.Metrics;
using SOFTURE.Common.Observability.Models;

namespace SOFTURE.Common.Observability.Tests.Support;

internal sealed class SourceInstruments : IDisposable
{
    private readonly Meter _serverMeter = new(RequestDurationInstrument.HttpServerRequests.SourceMeterName);
    private readonly Meter _clientMeter = new(RequestDurationInstrument.HttpClientRequests.SourceMeterName);
    private readonly Histogram<double> _serverDuration;
    private readonly Histogram<double> _clientDuration;

    public SourceInstruments()
    {
        _serverDuration = _serverMeter.CreateHistogram<double>(RequestDurationInstrument.HttpServerRequests.Name, "s");
        _clientDuration = _clientMeter.CreateHistogram<double>(RequestDurationInstrument.HttpClientRequests.Name, "s");
    }

    public void RecordServerRequest(string? route, int statusCode = 200, double seconds = 0.1)
    {
        var tags = new TagList
        {
            { "http.request.method", "GET" },
            { "http.response.status_code", statusCode },
            { "network.protocol.version", "1.1" },
            { "url.scheme", "http" }
        };

        if (route is not null)
        {
            tags.Add("http.route", route);
        }

        _serverDuration.Record(seconds, tags);
    }

    public void RecordClientRequest(string host, int port = 443, int statusCode = 200, string? errorType = null, double seconds = 0.1)
    {
        var tags = new TagList
        {
            { "http.request.method", "POST" },
            { "http.response.status_code", statusCode },
            { "server.address", host },
            { "server.port", port },
            { "url.scheme", port == 443 ? "https" : "http" },
            { "network.protocol.version", "1.1" }
        };

        if (errorType is not null)
        {
            tags.Add("error.type", errorType);
        }

        _clientDuration.Record(seconds, tags);
    }

    public void Dispose()
    {
        _serverMeter.Dispose();
        _clientMeter.Dispose();
    }
}
