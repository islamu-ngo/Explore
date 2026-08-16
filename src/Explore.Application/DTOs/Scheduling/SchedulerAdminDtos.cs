// ABOUTME: Read models for the instance scheduler administration surface.
// ABOUTME: Exposes scheduling metadata and fire timelines only, never job payloads or tenant content.

namespace Explore.Application.DTOs.Scheduling;

/// <summary>
/// Snapshot of the instance scheduler for operator tooling. When <see cref="Available"/> is false the host runs
/// with scheduling disabled and every collection is empty; clients render that as an explicit disabled state
/// rather than as a healthy scheduler with no work.
/// </summary>
public sealed class SchedulerAdminOverviewDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public bool Available { get; set; }

    /// <summary>True when the host refuses mutating scheduler actions regardless of caller permissions.</summary>
    public bool ReadOnly { get; set; }

    public string SchedulerName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>Normalized lifecycle token: <c>running</c>, <c>standby</c>, <c>shutdown</c>, or <c>disabled</c>.</summary>
    public string State { get; set; } = string.Empty;

    public bool Clustered { get; set; }

    /// <summary>
    /// True when the executing-job view and the interrupt action reflect only the node that served this request.
    /// Executing-job reads and interruption are answered per node, not across a cluster, so a clustered
    /// deployment must not read "not running" as "not running anywhere".
    /// </summary>
    public bool ExecutingViewIsNodeLocal { get; set; }

    /// <summary>Count of jobs whose triggers are in the scheduler's error state; drives readiness reporting.</summary>
    public int ErroredJobCount { get; set; }
    public bool SupportsPersistence { get; set; }
    public int ExecutingJobCount { get; set; }

    /// <summary>Number of jobs in the scheduler's store; the rows themselves are a separate collection resource.</summary>
    public int JobCount { get; set; }

    public int PausedJobCount { get; set; }

    /// <summary>Jobs the platform declares but has not scheduled, surfaced so gaps are visible to operators.</summary>
    public IReadOnlyList<string> PlannedJobs { get; set; } = [];
}

public sealed class SchedulerAdminJobDto
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;

    /// <summary>Owning subsystem from the platform job catalog; null for a job with no registry entry.</summary>
    public string? Owner { get; set; }

    public string? Description { get; set; }
    public bool Durable { get; set; }
    public bool Executing { get; set; }

    /// <summary>Aggregate of the job's trigger states: <c>paused</c> when every trigger is paused.</summary>
    public string State { get; set; } = string.Empty;

    public DateTimeOffset? NextFireTimeUtc { get; set; }
    public DateTimeOffset? PreviousFireTimeUtc { get; set; }
    public IReadOnlyList<SchedulerAdminTriggerDto> Triggers { get; set; } = [];
}

public sealed class SchedulerAdminTriggerDto
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    /// <summary>Human-readable cadence, such as a cron expression or a repeat interval.</summary>
    public string? ScheduleSummary { get; set; }

    public DateTimeOffset? NextFireTimeUtc { get; set; }
    public DateTimeOffset? PreviousFireTimeUtc { get; set; }
}
