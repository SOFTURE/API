using SOFTURE.Common.Observability.Extensions;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Export;

internal sealed class ExportOrigins
{
    private readonly Uri[] _origins;

    private ExportOrigins(Uri[] origins)
    {
        _origins = origins;
    }

    public static ExportOrigins From(IOpenTelemetrySettings settings)
    {
        var origins = new List<Uri> { settings.OpenTelemetry.GetEndpoint() };

        if (settings is ISeqSettings seqSettings)
        {
            origins.Add(GetSeqUrl(seqSettings.Seq));
        }

        return new ExportOrigins(origins.ToArray());
    }

    public bool Contains(Uri? requestUri)
    {
        return _origins.Any(origin => origin.IsSameOrigin(requestUri));
    }

    public bool Contains(string host, int? port)
    {
        return _origins.Any(origin =>
            string.Equals(origin.Host, host, StringComparison.OrdinalIgnoreCase)
            && (port is null || origin.Port == port));
    }

    private static Uri GetSeqUrl(SeqSettings settings)
    {
        return Uri.TryCreate(settings.Url.Trim(), UriKind.Absolute, out var url)
            ? url
            : throw new InvalidOperationException($"Invalid Seq:Url '{settings.Url}': expected an absolute URL.");
    }
}
