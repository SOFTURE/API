using Xunit;
using SOFTURE.Common.Observability.Export;
using SOFTURE.Common.Observability.Settings;
using SOFTURE.Common.Observability.Tests.Support;

namespace SOFTURE.Common.Observability.Tests.Export;

public sealed class ExportOriginsTests
{
    private readonly ExportOrigins _origins = ExportOrigins.From(MetricsHarness.CreateSettings(new MetricsSettings()));

    [Theory]
    [InlineData("http://localhost:4318/v1/traces")]
    [InlineData("http://localhost:5341/ingest/clef")]
    public void Request_to_an_export_origin_is_recognised(string requestUri)
    {
        Assert.True(_origins.Contains(new Uri(requestUri)));
    }

    [Theory]
    [InlineData("https://localhost:4318/v1/traces")]
    [InlineData("http://localhost:8080/api")]
    [InlineData("/relative")]
    public void Request_to_another_origin_is_not_recognised(string requestUri)
    {
        Assert.False(_origins.Contains(new Uri(requestUri, UriKind.RelativeOrAbsolute)));
    }

    [Fact]
    public void Host_and_port_of_an_export_origin_are_recognised()
    {
        Assert.True(_origins.Contains("localhost", 5341));
        Assert.True(_origins.Contains("localhost", null));
        Assert.False(_origins.Contains("localhost", 8080));
    }

    [Fact]
    public void Settings_without_seq_recognise_only_the_otlp_endpoint()
    {
        var origins = ExportOrigins.From(new OpenTelemetryOnlySettings
        {
            OpenTelemetry = MetricsHarness.CreateSettings(new MetricsSettings()).OpenTelemetry
        });

        Assert.True(origins.Contains("localhost", 4318));
        Assert.False(origins.Contains("localhost", 5341));
    }

    [Fact]
    public void Relative_seq_url_fails_with_the_setting_name()
    {
        var settings = MetricsHarness.CreateSettings(new MetricsSettings());

        var exception = Assert.Throws<InvalidOperationException>(() => ExportOrigins.From(new TestSettings
        {
            OpenTelemetry = settings.OpenTelemetry,
            Seq = new SeqSettings { Url = "seq-without-scheme", ApiKey = string.Empty }
        }));

        Assert.Equal("Invalid Seq:Url 'seq-without-scheme': expected an absolute URL.", exception.Message);
    }

    private sealed class OpenTelemetryOnlySettings : IOpenTelemetrySettings
    {
        public required OpenTelemetrySettings OpenTelemetry { get; init; }
    }
}
