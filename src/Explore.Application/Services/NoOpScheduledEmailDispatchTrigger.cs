// ABOUTME: Default scheduler trigger used when no infrastructure scheduler is active.
// ABOUTME: Allows durable outbox creation to continue while cron drains remain the recovery path.

using Explore.Application.Contracts.Infrastructure;

namespace Explore.Application.Services;

public sealed class NoOpScheduledEmailDispatchTrigger : IScheduledEmailDispatchTrigger
{
    public Task<ScheduledEmailDispatchTriggerResult> ScheduleAsync(
        ScheduledEmailDispatchPointer pointer,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ScheduledEmailDispatchTriggerResult.NotScheduled("scheduler_disabled"));
    }
}
