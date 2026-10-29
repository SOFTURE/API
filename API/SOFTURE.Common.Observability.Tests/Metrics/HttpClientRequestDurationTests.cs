using Xunit;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;
using SOFTURE.Common.Observability.Tests.Support;

namespace SOFTURE.Common.Observability.Tests.Metrics;

public sealed class HttpClientRequestDurationTests
{
    private const string Name = "http.client.request.duration";

    [Fact]
    public void Requests_to_the_otlp_endpoint_and_seq_are_not_exported()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings());
        using var source = new SourceInstruments();

        source.RecordClientRequest("localhost", port: 4318);
        source.RecordClientRequest("LOCALHOST", port: 5341);
        source.RecordClientRequest("api.example.com");

        var metric = harness.Collect().Find(Name);

        Assert.NotNull(metric);
        Assert.Equal(["api.example.com"], metric.GetPointTagValues("server.address"));
    }

    [Fact]
    public void Requests_to_the_same_host_on_another_port_are_exported()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings());
        using var source = new SourceInstruments();

        source.RecordClientRequest("localhost", port: 8080);

        var metric = harness.Collect().Find(Name);

        Assert.NotNull(metric);
        Assert.Equal(["8080"], metric.GetPointTagValues("server.port"));
    }

    [Fact]
    public void Exports_the_relayed_histogram_instead_of_the_source_one()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings());
        using var source = new SourceInstruments();

        source.RecordClientRequest("api.example.com");

        var metric = harness.Collect().Single(snapshot => snapshot.Name == Name);

        Assert.Equal(RequestDurationInstrument.RelayMeterName, metric.MeterName);
        Assert.Equal("s", metric.Unit);
    }

    [Fact]
    public void Minimal_profile_drops_certificate_revocation_hosts()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings());
        using var source = new SourceInstruments();

        source.RecordClientRequest("crl3.digicert.com", port: 80);
        source.RecordClientRequest("ocsp.sectigo.com", port: 80);
        source.RecordClientRequest("r11.c.lencr.org", port: 80);
        source.RecordClientRequest("c.pki.goog", port: 80);
        source.RecordClientRequest("api.example.com");

        var metric = harness.Collect().Find(Name);

        Assert.NotNull(metric);
        Assert.Equal(["api.example.com"], metric.GetPointTagValues("server.address"));
    }

    [Fact]
    public void Full_profile_keeps_certificate_revocation_hosts()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { Profile = MetricsProfile.Full });
        using var source = new SourceInstruments();

        source.RecordClientRequest("crl3.digicert.com", port: 80);
        source.RecordClientRequest("localhost", port: 4318);

        var metric = harness.Collect().Find(Name);

        Assert.NotNull(metric);
        Assert.Equal(["crl3.digicert.com"], metric.GetPointTagValues("server.address"));
    }

    [Fact]
    public void Excluded_hosts_are_not_exported()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings
        {
            Profile = MetricsProfile.Full,
            ExcludedHttpClientHosts = ["seq", "*.internal"]
        });
        using var source = new SourceInstruments();

        source.RecordClientRequest("seq", port: 80);
        source.RecordClientRequest("auth.internal", port: 3000);
        source.RecordClientRequest("api.example.com");

        var metric = harness.Collect().Find(Name);

        Assert.NotNull(metric);
        Assert.Equal(["api.example.com"], metric.GetPointTagValues("server.address"));
    }

    [Fact]
    public void Disabled_instrumentation_exports_no_client_request_duration()
    {
        using var harness = MetricsHarness.Start(new MetricsSettings { HttpClientEnabled = false });
        using var source = new SourceInstruments();

        source.RecordClientRequest("api.example.com");

        Assert.Null(harness.Collect().Find(Name));
    }
}
