// ABOUTME: Centralized Quartz job and trigger keys derived from the Application scheduled-job catalog.
// ABOUTME: Keeps scheduler identity stable across restarts so the persistent store recognizes existing rows.

using Explore.Application.Contracts.Scheduling;
using Quartz;

namespace Explore.API.Scheduling;

/// <summary>
/// Job keys are persisted in the ADO job store, so they must never change for an existing deployment.
/// They are derived from <see cref="ScheduledJobNames"/> so the catalog stays the single source of naming truth.
/// </summary>
public static class QuartzSchedulerKeys
{
    /// <summary>Group for platform-owned recurring maintenance work.</summary>
    public const string RecurringGroup = "platform-recurring";

    /// <summary>Group for one-off triggers scheduled at runtime from application code.</summary>
    public const string OnDemandGroup = "platform-on-demand";

    /// <summary>Quartz requires <c>?</c> in either day-of-month or day-of-week; the catalog stores the same text.</summary>
    public const string EmailDispatchDrainCron = "*/10 * * * * ?";

    public const string EmailDispatchRecoveryScanCron = "0 */1 * * * ?";

    /// <summary><see cref="JobDataMap"/> entry holding the JSON-serialized dispatch pointer.</summary>
    public const string DispatchPointerDataKey = "dispatchPointer";

    public static readonly JobKey EmailDispatchDrain =
        new(ScheduledJobNames.EmailDispatchDrain, RecurringGroup);

    public static readonly JobKey EmailDispatchRecoveryScan =
        new(ScheduledJobNames.EmailDispatchRecoveryScan, RecurringGroup);

    public static readonly JobKey EventReminderDispatch =
        new(ScheduledJobNames.EventReminderDispatch, OnDemandGroup);

    public static TriggerKey RecurringTriggerFor(JobKey jobKey)
    {
        ArgumentNullException.ThrowIfNull(jobKey);
        return new TriggerKey(jobKey.Name, jobKey.Group);
    }
}
