// ABOUTME: API-host configuration for TickerQ scheduler persistence, dashboard, and node identity.
// ABOUTME: Keeps scheduler operations separate from domain-owned outbox delivery state.

namespace Explore.API.Configuration;

public sealed class TickerQSchedulerOptions
{
    public const string SectionName = "Scheduler:TickerQ";
    public const string InstanceAdminPolicyName = "tickerq_instance_admin";

    public bool Enabled { get; set; } = true;
    public string Schema { get; set; } = "ticker";
    public int MaxConcurrency { get; set; } = Math.Max(1, Environment.ProcessorCount);
    public string NodeIdentifier { get; set; } = Environment.MachineName;
    public bool DashboardEnabled { get; set; }
    public string DashboardPath { get; set; } = "/admin/scheduler";
    public string DashboardAuthorizationPolicy { get; set; } = InstanceAdminPolicyName;
    public int DashboardSessionTimeoutMinutes { get; set; } = 30;
}
