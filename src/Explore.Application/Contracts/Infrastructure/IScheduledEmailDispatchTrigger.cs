// ABOUTME: Infrastructure port for asking a scheduler to wake a persisted EmailDispatchOutbox row.
// ABOUTME: Keeps Application independent from TickerQ while allowing delayed lifecycle work to be accelerated.

namespace Explore.Application.Contracts.Infrastructure;

public interface IScheduledEmailDispatchTrigger
{
    Task<ScheduledEmailDispatchTriggerResult> ScheduleAsync(
        ScheduledEmailDispatchPointer pointer,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken);
}

public sealed record ScheduledEmailDispatchTriggerResult(
    bool Scheduled,
    Guid? SchedulerJobId,
    string FailureCategory)
{
    public static ScheduledEmailDispatchTriggerResult Success(Guid schedulerJobId)
    {
        return new ScheduledEmailDispatchTriggerResult(true, schedulerJobId, "none");
    }

    public static ScheduledEmailDispatchTriggerResult NotScheduled(string failureCategory)
    {
        return new ScheduledEmailDispatchTriggerResult(false, null, failureCategory);
    }
}
