// ABOUTME: Stable operational job names for platform-owned scheduled work.
// ABOUTME: Keeps scheduler identifiers centralized so scheduler job names do not drift from Application contracts.

namespace Explore.Application.Contracts.Scheduling;

public static class ScheduledJobNames
{
    public const string EmailDispatchDrain = "email-dispatch-drain";
    public const string GeneralOutboxDrain = "general-outbox-drain";
    public const string PdsSyncDrain = "pds-sync-drain";
    public const string EmailDispatchRecoveryScan = "email-dispatch-recovery-scan";
    public const string DeadLetterSummary = "dead-letter-summary";
    public const string EventReminderDispatch = "event-reminder-dispatch";
    public const string WaitlistPromotionScan = "waitlist-promotion-scan";
    public const string TenantMaintenanceScan = "tenant-maintenance-scan";
}
