// ABOUTME: Unit tests for the scheduler-neutral job registry.
// ABOUTME: Guards stable job names and pointer-only scheduling metadata for future operator surfaces.

using Explore.Application.Contracts.Scheduling;
using Explore.Application.Services;
using TUnit.Core;

namespace Event.Application.UnitTests.Services;

public sealed class ScheduledJobRegistryTests
{
    [Test]
    public async Task ListJobsIncludesImplementedEmailDispatchAndEventReminderContracts()
    {
        var registry = new ScheduledJobRegistry();

        var jobs = registry.ListJobs();
        var emailDrain = registry.FindByName(ScheduledJobNames.EmailDispatchDrain);
        var emailRecovery = registry.FindByName(ScheduledJobNames.EmailDispatchRecoveryScan);
        var eventReminder = registry.FindByName(ScheduledJobNames.EventReminderDispatch);

        await Assert.That(jobs.Count).IsGreaterThanOrEqualTo(8);
        await Assert.That(emailDrain).IsNotNull();
        await Assert.That(emailDrain!.ScheduleKind).IsEqualTo(ScheduledJobScheduleKind.Cron);
        await Assert.That(emailDrain.PayloadKind).IsEqualTo(ScheduledJobPayloadKind.None);
        await Assert.That(emailDrain.Status).IsEqualTo(ScheduledJobOperationalStatus.Implemented);
        await Assert.That(emailDrain.CronExpression).IsEqualTo("*/10 * * * * *");
        await Assert.That(emailRecovery).IsNotNull();
        await Assert.That(emailRecovery!.ScheduleKind).IsEqualTo(ScheduledJobScheduleKind.Cron);
        await Assert.That(emailRecovery.PayloadKind).IsEqualTo(ScheduledJobPayloadKind.None);
        await Assert.That(emailRecovery.Status).IsEqualTo(ScheduledJobOperationalStatus.Implemented);
        await Assert.That(emailRecovery.CronExpression).IsEqualTo("0 */1 * * * *");
        await Assert.That(eventReminder).IsNotNull();
        await Assert.That(eventReminder!.ScheduleKind).IsEqualTo(ScheduledJobScheduleKind.Time);
        await Assert.That(eventReminder.PayloadKind).IsEqualTo(ScheduledJobPayloadKind.PointerOnly);
        await Assert.That(eventReminder.Status).IsEqualTo(ScheduledJobOperationalStatus.Implemented);
    }

    [Test]
    public async Task PlannedOutboxAndPdsJobsRemainCatalogedButNotImplemented()
    {
        var registry = new ScheduledJobRegistry();

        var generalOutbox = registry.FindByName(ScheduledJobNames.GeneralOutboxDrain);
        var pdsSync = registry.FindByName(ScheduledJobNames.PdsSyncDrain);

        await Assert.That(generalOutbox).IsNotNull();
        await Assert.That(generalOutbox!.Status).IsEqualTo(ScheduledJobOperationalStatus.Planned);
        await Assert.That(generalOutbox.PayloadKind).IsEqualTo(ScheduledJobPayloadKind.None);
        await Assert.That(pdsSync).IsNotNull();
        await Assert.That(pdsSync!.Status).IsEqualTo(ScheduledJobOperationalStatus.Planned);
        await Assert.That(pdsSync.PayloadKind).IsEqualTo(ScheduledJobPayloadKind.None);
    }
}
