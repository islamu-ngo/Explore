// ABOUTME: API-host configuration for Quartz.NET scheduler persistence, clustering, and the operator status endpoint.
// ABOUTME: Keeps scheduler operations separate from domain-owned outbox delivery state.

namespace Explore.API.Configuration;

public sealed class QuartzSchedulerSettings
{
    public const string SectionName = "Scheduler:Quartz";
    public const string InstanceAdminPolicyName = "quartz_instance_admin";
    public const string DefaultTablePrefix = "QRTZ_";
    public const string DefaultStatusEndpointPath = "/admin/scheduler";
    public const string AutoInstanceId = "AUTO";

    public bool Enabled { get; set; } = true;

    /// <summary>Scheduler name recorded in the persistent store; must be stable across restarts of one logical instance.</summary>
    public string SchedulerName { get; set; } = "islamu-event-scheduler";

    /// <summary><c>AUTO</c> lets Quartz generate a unique per-process id, which is required for clustering.</summary>
    public string InstanceId { get; set; } = AutoInstanceId;

    public int MaxConcurrency { get; set; } = Math.Max(1, Environment.ProcessorCount);

    /// <summary>When false the scheduler runs on the in-memory store and loses state on restart.</summary>
    public bool UsePersistentStore { get; set; } = true;

    /// <summary>Prefix for the co-located scheduler tables in the primary application database.</summary>
    public string TablePrefix { get; set; } = DefaultTablePrefix;

    /// <summary>Enables the database-backed clustering protocol for multi-instance deployments.</summary>
    public bool ClusteringEnabled { get; set; }

    public int ClusterCheckinIntervalSeconds { get; set; } = 20;

    /// <summary>Applies the idempotent scheduler DDL at startup; disable when a migration job owns schema.</summary>
    public bool ApplySchemaOnStartup { get; set; } = true;

    public bool StatusEndpointEnabled { get; set; }

    public string StatusEndpointPath { get; set; } = DefaultStatusEndpointPath;

    public string StatusEndpointAuthorizationPolicy { get; set; } = InstanceAdminPolicyName;
}
