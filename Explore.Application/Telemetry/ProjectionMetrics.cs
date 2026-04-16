// ABOUTME: Custom OpenTelemetry metrics for EAV custom property projection operations.
// ABOUTME: Tracks rebuild/drain counts, durations, and failures with tenant + projection type dimensions.

using System.Diagnostics.Metrics;

namespace Explore.Application.Telemetry;

public sealed class ProjectionMetrics
{
    public const string MeterName = "Explore.Projections";

    private readonly Counter<long> _rebuildTotal;
    private readonly Counter<long> _rebuildFailuresTotal;
    private readonly Histogram<double> _rebuildDuration;
    private readonly Counter<long> _drainTotal;
    private readonly Counter<long> _drainFailuresTotal;
    private readonly Histogram<double> _drainDuration;
    private readonly Counter<long> _drainedScopesTotal;

    public ProjectionMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _rebuildTotal = meter.CreateCounter<long>(
            "explore.projections.rebuild_total",
            unit: "{rebuild}",
            description: "Total projection rebuild operations");

        _rebuildFailuresTotal = meter.CreateCounter<long>(
            "explore.projections.rebuild_failures_total",
            unit: "{failure}",
            description: "Total projection rebuild failures (rows that failed during rebuild)");

        _rebuildDuration = meter.CreateHistogram<double>(
            "explore.projections.rebuild_duration_seconds",
            unit: "s",
            description: "Projection rebuild duration in seconds");

        _drainTotal = meter.CreateCounter<long>(
            "explore.projections.drain_total",
            unit: "{drain}",
            description: "Total dirty-scope drain operations");

        _drainFailuresTotal = meter.CreateCounter<long>(
            "explore.projections.drain_failures_total",
            unit: "{failure}",
            description: "Total dirty-scope drain failures");

        _drainDuration = meter.CreateHistogram<double>(
            "explore.projections.drain_duration_seconds",
            unit: "s",
            description: "Dirty-scope drain duration in seconds");

        _drainedScopesTotal = meter.CreateCounter<long>(
            "explore.projections.drained_scopes_total",
            unit: "{scope}",
            description: "Total individual dirty scopes drained across all drain operations");
    }

    public void RecordRebuild(string tenantId, string projectionType, long rowsProcessed, long rowsFailed, double durationSeconds, bool lockAcquired)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("tenant_id", tenantId),
            new KeyValuePair<string, object?>("projection_type", projectionType),
            new KeyValuePair<string, object?>("lock_acquired", lockAcquired.ToString().ToLowerInvariant())
        };

        _rebuildTotal.Add(1, tags);
        _rebuildDuration.Record(durationSeconds, tags);

        if (rowsFailed > 0)
            _rebuildFailuresTotal.Add(rowsFailed, tags);
    }

    public void RecordDrain(string tenantId, string projectionType, int drainedCount, double durationSeconds)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("tenant_id", tenantId),
            new KeyValuePair<string, object?>("projection_type", projectionType)
        };

        _drainTotal.Add(1, tags);
        _drainDuration.Record(durationSeconds, tags);

        if (drainedCount > 0)
            _drainedScopesTotal.Add(drainedCount, tags);
    }

    public void RecordDrainFailure(string tenantId, string projectionType)
    {
        _drainFailuresTotal.Add(1,
            new KeyValuePair<string, object?>("tenant_id", tenantId),
            new KeyValuePair<string, object?>("projection_type", projectionType));
    }
}
