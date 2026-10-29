using System.Diagnostics;
using System.Diagnostics.Metrics;
using SOFTURE.Common.Observability.Abstractions;
using SOFTURE.Common.Observability.Models;
using SOFTURE.Common.Observability.Settings;

namespace SOFTURE.Common.Observability.Relays;

internal sealed class RequestDurationRelay : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly Meter _meter = new(RequestDurationInstrument.RelayMeterName);
    private readonly Histogram<double> _duration;
    private readonly IRequestFilter _filter;
    private readonly bool _groupStatusCodes;
    private readonly HashSet<string> _droppedTags;

    public RequestDurationRelay(RequestDurationInstrument instrument, IRequestFilter filter, MetricsSettings settings)
    {
        _duration = _meter.CreateHistogram<double>(instrument.Name, "s", instrument.Description);
        _filter = filter;
        _groupStatusCodes = settings.GroupStatusCodes;
        _droppedTags = new HashSet<string>(settings.DroppedRequestDurationTags, StringComparer.Ordinal);

        _listener.InstrumentPublished = (published, listener) =>
        {
            if (instrument.IsSource(published))
            {
                listener.EnableMeasurementEvents(published);
            }
        };
        _listener.SetMeasurementEventCallback<double>(OnMeasurement);
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    private void OnMeasurement(Instrument source, double seconds, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        if (_filter.IsExcluded(tags))
        {
            return;
        }

        _duration.Record(seconds, Shape(tags));
    }

    private TagList Shape(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var shaped = new TagList();

        foreach (var tag in tags)
        {
            if (_droppedTags.Contains(tag.Key))
            {
                continue;
            }

            if (_groupStatusCodes && tag.Key == RequestTags.StatusCode)
            {
                if (RequestTags.ToStatusClass(tag.Value) is { } statusClass)
                {
                    shaped.Add(RequestTags.StatusClass, statusClass);
                }

                continue;
            }

            if (_groupStatusCodes && tag.Key == RequestTags.ErrorType && RequestTags.ToStatusClass(tag.Value) is { } errorClass)
            {
                shaped.Add(RequestTags.ErrorType, errorClass);
                continue;
            }

            shaped.Add(tag);
        }

        return shaped;
    }
}
