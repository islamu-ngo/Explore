// ABOUTME: Default catalog of platform-owned scheduled jobs.
// ABOUTME: Separates implemented scheduler work from planned migrations while preserving stable job names.

using Explore.Application.Contracts.Scheduling;

namespace Explore.Application.Services;

public sealed class ScheduledJobRegistry : IScheduledJobRegistry
{
    private static readonly ScheduledJobDescriptor[] Jobs =
    [
        new(
            ScheduledJobNames.EmailDispatchDrain,
            "EmailDispatch",
            ScheduledJobScheduleKind.Cron,
            ScheduledJobPayloadKind.None,
            ScheduledJobOperationalStatus.Implemented,
            "Claims due EmailDispatchOutbox rows and executes approved dispatch transports.",
            "*/10 * * * * ?"),
        new(
            ScheduledJobNames.EventReminderDispatch,
            "EventLifecycle",
            ScheduledJobScheduleKind.Time,
            ScheduledJobPayloadKind.PointerOnly,
            ScheduledJobOperationalStatus.Implemented,
            "Wakes a pre-persisted event reminder EmailDispatchOutbox row at its scheduled time."),
        new(
            ScheduledJobNames.GeneralOutboxDrain,
            "Outbox",
            ScheduledJobScheduleKind.Cron,
            ScheduledJobPayloadKind.None,
            ScheduledJobOperationalStatus.Planned,
            "Future drain for general integration outbox work after EmailDispatch proves multi-node behavior."),
        new(
            ScheduledJobNames.PdsSyncDrain,
            "PDS",
            ScheduledJobScheduleKind.Cron,
            ScheduledJobPayloadKind.None,
            ScheduledJobOperationalStatus.Planned,
            "Future AT Protocol PDS sync drain using existing durable PDS state."),
        new(
            ScheduledJobNames.EmailDispatchRecoveryScan,
            "EmailDispatch",
            ScheduledJobScheduleKind.Cron,
            ScheduledJobPayloadKind.None,
            ScheduledJobOperationalStatus.Implemented,
            "Marks stale EmailDispatchOutbox processing leases as Unknown for operator review.",
            "0 */1 * * * ?"),
        new(
            ScheduledJobNames.DeadLetterSummary,
            "Operations",
            ScheduledJobScheduleKind.Cron,
            ScheduledJobPayloadKind.None,
            ScheduledJobOperationalStatus.Planned,
            "Future operator summary for dead-lettered platform work."),
        new(
            ScheduledJobNames.WaitlistPromotionScan,
            "EventLifecycle",
            ScheduledJobScheduleKind.Cron,
            ScheduledJobPayloadKind.None,
            ScheduledJobOperationalStatus.Planned,
            "Future waitlist promotion scanner that creates durable domain intents before side effects."),
        new(
            ScheduledJobNames.InventoryHoldExpiry,
            "Registration",
            ScheduledJobScheduleKind.Time,
            ScheduledJobPayloadKind.PointerOnly,
            ScheduledJobOperationalStatus.Implemented,
            "Expires one registration order's due capacity holds at the order's earliest hold expiry."),
        new(
            ScheduledJobNames.InventoryHoldExpiryReconciliation,
            "Registration",
            ScheduledJobScheduleKind.Cron,
            ScheduledJobPayloadKind.None,
            ScheduledJobOperationalStatus.Implemented,
            "Safety-net sweep for expired holds and lifecycle recovery targets no deadline covered.",
            "0 */5 * * * ?"),
        new(
            ScheduledJobNames.RegistrationFinalizationDrain,
            "Registration",
            ScheduledJobScheduleKind.Cron,
            ScheduledJobPayloadKind.None,
            ScheduledJobOperationalStatus.Implemented,
            "Drains durable registration-finalization effects under the shared fenced claim.",
            "*/10 * * * * ?"),
        new(
            ScheduledJobNames.TenantMaintenanceScan,
            "Operations",
            ScheduledJobScheduleKind.Cron,
            ScheduledJobPayloadKind.None,
            ScheduledJobOperationalStatus.Planned,
            "Future tenant maintenance scanner for scheduled platform controls.")
    ];

    public IReadOnlyCollection<ScheduledJobDescriptor> ListJobs()
    {
        return Jobs;
    }

    public ScheduledJobDescriptor? FindByName(string name)
    {
        return Jobs.FirstOrDefault(job => string.Equals(job.Name, name, StringComparison.Ordinal));
    }
}
