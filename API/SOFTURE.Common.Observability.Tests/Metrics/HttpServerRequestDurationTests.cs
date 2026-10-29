using Xunit;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;
using SOFTURE.Common.Observability.Tests.Support;

namespace SOFTURE.Common.Observability.Tests.Metrics;

public sealed class HttpServerRequestDurationTests
{
    private const string Name = "http.server.request.duration";

    [Fact]
    public void Exports_every_route_by_default()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings());
        using var source = new SourceInstruments();

        source.RecordServerRequest("/hc");
        source.RecordServerRequest("pets/{id}");
        source.RecordServerRequest(null);

        var metric = harness.Collect().Single(snapshot => snapshot.Name == Name);

        Assert.Equal(RequestDurationInstrument.RelayMeterName, metric.MeterName);
        Assert.Equal([null, "/hc", "pets/{id}"], metric.GetPointTagValues("http.route"));
    }

    [Fact]
    public void Excluded_routes_are_not_exported_with_or_without_a_leading_slash()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { ExcludedRoutes = ["/hc", "openapi*", "*/ping"] });
        using var source = new SourceInstruments();

        source.RecordServerRequest("hc");
        source.RecordServerRequest("/openapi/{documentName}.json");
        source.RecordServerRequest("/internal/ping");
        source.RecordServerRequest("/pets/{id}");

        var metric = harness.Collect().Find(Name);

        Assert.NotNull(metric);
        Assert.Equal(["/pets/{id}"], metric.GetPointTagValues("http.route"));
    }

    [Fact]
    public void Unmatched_requests_are_not_exported_when_excluded()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { ExcludeUnmatchedRoutes = true });
        using var source = new SourceInstruments();

        source.RecordServerRequest(null, statusCode: 404);
        source.RecordServerRequest(string.Empty, statusCode: 404);
        source.RecordServerRequest("/pets");

        var metric = harness.Collect().Find(Name);

        Assert.NotNull(metric);
        Assert.Equal(["/pets"], metric.GetPointTagValues("http.route"));
    }

    [Fact]
    public void Disabled_instrumentation_exports_no_server_request_duration()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { AspNetCoreEnabled = false });
        using var source = new SourceInstruments();

        source.RecordServerRequest("/pets");

        Assert.Null(harness.Collect().Find(Name));
    }
}
