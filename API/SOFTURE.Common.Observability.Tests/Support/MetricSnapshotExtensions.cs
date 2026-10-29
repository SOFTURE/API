using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace SOFTURE.Common.Observability.Tests.Support;

internal static class MetricSnapshotExtensions
{
    public static MetricSnapshot? Find(this IEnumerable<MetricSnapshot> metrics, string name)
    {
        return metrics.SingleOrDefault(metric => metric.Name == name);
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> GetPointTags(this MetricSnapshot metric)
    {
        return metric.MetricPoints
            .Select(point => ToDictionary(point.Tags))
            .ToList();
    }

    public static IReadOnlyList<string?> GetPointTagValues(this MetricSnapshot metric, string key)
    {
        return metric.GetPointTags()
            .Select(tags => tags.TryGetValue(key, out var value) ? value?.ToString() : null)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<double> GetBucketBoundaries(this MetricSnapshot metric)
    {
        var boundaries = new List<double>();

        foreach (var bucket in metric.MetricPoints[0].GetHistogramBuckets())
        {
            boundaries.Add(bucket.ExplicitBound);
        }

        return boundaries;
    }

    private static IReadOnlyDictionary<string, object?> ToDictionary(ReadOnlyTagCollection tags)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var tag in tags)
        {
            result[tag.Key] = tag.Value;
        }

        return result;
    }
}
