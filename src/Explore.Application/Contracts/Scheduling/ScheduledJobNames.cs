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

    // Periodic maintenance sweeps. These names appear in scheduler rows and operator tooling, so they are
    // stable identifiers rather than descriptions: renaming one orphans its persisted trigger.
    public const string IdempotencyCleanup = "idempotency-cleanup";
    public const string AiRetentionCleanup = "ai-retention-cleanup";
    public const string EmailDispatchRetentionCleanup = "email-dispatch-retention-cleanup";
    public const string WebhookRetentionCleanup = "webhook-retention-cleanup";
    public const string PrivacyErasureCredentialCleanup = "privacy-erasure-credential-cleanup";
    public const string StorageReconciliation = "storage-reconciliation";
    public const string RegistrationRetentionCleanup = "registration-retention-cleanup";
    public const string OrganizerPaymentReadinessReconciliation = "organizer-payment-readiness-reconciliation";
}
