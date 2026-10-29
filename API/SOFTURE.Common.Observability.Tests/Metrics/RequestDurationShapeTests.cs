using Xunit;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;
using SOFTURE.Common.Observability.Tests.Support;

namespace SOFTURE.Common.Observability.Tests.Metrics;

public sealed class RequestDurationShapeTests
{
    private const string ServerName = "http.server.request.duration";
    private const string ClientName = "http.client.request.duration";

    [Fact]
    public void Grouping_status_codes_replaces_the_code_with_its_class()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { GroupStatusCodes = true });
        using var source = new SourceInstruments();

        source.RecordServerRequest("/pets", statusCode: 200);
        source.RecordServerRequest("/pets", statusCode: 204);
        source.RecordServerRequest("/pets", statusCode: 404);
        source.RecordServerRequest("/pets", statusCode: 503);
        source.RecordClientRequest("api.example.com", statusCode: 302);

        var metrics = harness.Collect();
        var server = metrics.Find(ServerName);
        var client = metrics.Find(ClientName);

        Assert.NotNull(server);
        Assert.NotNull(client);
        Assert.Equal(["2xx", "4xx", "5xx"], server.GetPointTagValues("http.response.status_class"));
        Assert.All(server.GetPointTags(), tags => Assert.False(tags.ContainsKey("http.response.status_code")));
        Assert.Equal(["3xx"], client.GetPointTagValues("http.response.status_class"));
    }

    [Fact]
    public void Grouping_status_codes_also_groups_numeric_error_types_and_keeps_named_ones()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { GroupStatusCodes = true });
        using var source = new SourceInstruments();

        source.RecordClientRequest("api.example.com", statusCode: 404, errorType: "404");
        source.RecordClientRequest("api.example.com", statusCode: 410, errorType: "410");
        source.RecordClientRequest("api.example.com", statusCode: 0, errorType: "connection_error");

        var metric = harness.Collect().Find(ClientName);

        Assert.NotNull(metric);
        Assert.Equal(["4xx", "connection_error"], metric.GetPointTagValues("error.type"));
    }

    [Fact]
    public void Error_types_stay_exact_without_grouping()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings());
        using var source = new SourceInstruments();

        source.RecordClientRequest("api.example.com", statusCode: 404, errorType: "404");

        var metric = harness.Collect().Find(ClientName);

        Assert.NotNull(metric);
        Assert.Equal(["404"], metric.GetPointTagValues("error.type"));
    }

    [Fact]
    public void Grouping_status_codes_leaves_out_codes_outside_100_to_599()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { GroupStatusCodes = true });
        using var source = new SourceInstruments();

        source.RecordClientRequest("api.example.com", statusCode: 0);

        var metric = harness.Collect().Find(ClientName);

        Assert.NotNull(metric);
        var tags = Assert.Single(metric.GetPointTags());
        Assert.False(tags.ContainsKey("http.response.status_class"));
        Assert.False(tags.ContainsKey("http.response.status_code"));
    }

    [Fact]
    public void Status_codes_stay_exact_by_default()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings());
        using var source = new SourceInstruments();

        source.RecordServerRequest("/pets", statusCode: 200);
        source.RecordServerRequest("/pets", statusCode: 204);

        var metric = harness.Collect().Find(ServerName);

        Assert.NotNull(metric);
        Assert.Equal(["200", "204"], metric.GetPointTagValues("http.response.status_code"));
    }

    [Fact]
    public void Dropped_tags_are_removed_from_both_histograms()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings
        {
            DroppedRequestDurationTags = ["network.protocol.version", "url.scheme"]
        });
        using var source = new SourceInstruments();

        source.RecordServerRequest("/pets");
        source.RecordClientRequest("api.example.com");

        var metrics = harness.Collect();
        var server = metrics.Find(ServerName);
        var client = metrics.Find(ClientName);

        Assert.NotNull(server);
        Assert.NotNull(client);
        Assert.Equal(
            ["http.request.method", "http.response.status_code", "http.route"],
            Assert.Single(server.GetPointTags()).Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(
            ["http.request.method", "http.response.status_code", "server.address", "server.port"],
            Assert.Single(client.GetPointTags()).Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void Minimal_profile_uses_four_request_duration_boundaries()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings());
        using var source = new SourceInstruments();

        source.RecordServerRequest("/pets");
        source.RecordClientRequest("api.example.com");

        var metrics = harness.Collect();

        Assert.Equal([0.05, 0.25, 1, 5, double.PositiveInfinity], metrics.Find(ServerName)!.GetBucketBoundaries());
        Assert.Equal([0.05, 0.25, 1, 5, double.PositiveInfinity], metrics.Find(ClientName)!.GetBucketBoundaries());
    }

    [Fact]
    public void Full_profile_keeps_the_default_request_duration_boundaries()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { Profile = MetricsProfile.Full });
        using var source = new SourceInstruments();

        source.RecordServerRequest("/pets");

        var metric = harness.Collect().Find(ServerName);

        Assert.NotNull(metric);
        Assert.Equal(
            [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10, double.PositiveInfinity],
            metric.GetBucketBoundaries());
    }

    [Fact]
    public void Configured_boundaries_override_the_profile_default()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings
        {
            Profile = MetricsProfile.Full,
            RequestDurationBoundaries = [0.1, 1]
        });
        using var source = new SourceInstruments();

        source.RecordServerRequest("/pets");

        var metric = harness.Collect().Find(ServerName);

        Assert.NotNull(metric);
        Assert.Equal([0.1, 1, double.PositiveInfinity], metric.GetBucketBoundaries());
    }

    [Theory]
    [InlineData(new[] { 1.0, 0.5 })]
    [InlineData(new[] { 0.5, 0.5 })]
    [InlineData(new[] { 0.5, double.NaN })]
    public void Boundaries_out_of_ascending_order_fail_at_registration(double[] boundaries)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MetricsHarness.Start(new MetricsSettings { RequestDurationBoundaries = boundaries }));

        Assert.StartsWith("Invalid OpenTelemetry:Metrics:RequestDurationBoundaries", exception.Message);
    }

    [Fact]
    public void Dropped_instruments_also_drop_the_relayed_histogram()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { DroppedInstruments = [ClientName] });
        using var source = new SourceInstruments();

        source.RecordServerRequest("/pets");
        source.RecordClientRequest("api.example.com");

        var metrics = harness.Collect();

        Assert.NotNull(metrics.Find(ServerName));
        Assert.Null(metrics.Find(ClientName));
    }
}
