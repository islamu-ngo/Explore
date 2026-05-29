// ABOUTME: Unit tests for delayed Event lifecycle scheduler orchestration.
// ABOUTME: Proves reminders persist EmailDispatchOutbox state before requesting scheduler wake-ups.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Services;

public sealed class EventLifecycleSchedulerTests
{
    [Test]
    public async Task ScheduleEventReminderCreatesOutboxBeforePointerTrigger()
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var registrationIntentId = Guid.CreateVersion7();
        var outboxId = Guid.CreateVersion7();
        var dispatchAt = DateTimeOffset.UtcNow.AddHours(2);
        var repository = Substitute.For<IEmailDispatchOutboxRepository>();
        var trigger = Substitute.For<IScheduledEmailDispatchTrigger>();
        var scheduler = new EventLifecycleScheduler(
            new EventLifecycleEmailOutboxFactory(),
            repository,
            trigger);

        repository.Create(Arg.Any<EmailDispatchOutbox>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var outbox = call.Arg<EmailDispatchOutbox>();
                outbox.Id = outboxId;
                return outbox;
            });
        trigger.ScheduleAsync(
                Arg.Any<ScheduledEmailDispatchPointer>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(ScheduledEmailDispatchTriggerResult.Success(Guid.CreateVersion7()));

        var result = await scheduler.ScheduleEventReminderAsync(
            new EventReminderScheduleInput(
                tenantId,
                userId,
                eventId,
                registrationIntentId,
                "attendee@example.test",
                "Community Dinner",
                DateTimeOffset.UtcNow.AddDays(3),
                dispatchAt),
            CancellationToken.None);

        await repository.Received(1).Create(
            Arg.Is<EmailDispatchOutbox>(outbox =>
                outbox.TenantId == tenantId
                && outbox.Kind == EmailDispatchKind.EventReminder
                && outbox.Status == EmailDispatchStatus.Pending
                && outbox.NextAttemptAt == dispatchAt.UtcDateTime
                && outbox.EventId == eventId
                && outbox.RegistrationIntentId == registrationIntentId
                && outbox.UserId == userId),
            Arg.Any<CancellationToken>());
        await trigger.Received(1).ScheduleAsync(
            Arg.Is<ScheduledEmailDispatchPointer>(pointer =>
                pointer.TenantId == tenantId
                && pointer.PublishEventId == result.PublishEventId
                && pointer.UseCase == EventLifecycleAutomationUseCases.EventReminder
                && pointer.EventId == eventId
                && pointer.RegistrationIntentId == registrationIntentId
                && pointer.UserId == userId),
            dispatchAt,
            Arg.Any<CancellationToken>());
        await Assert.That(result.EmailDispatchOutboxId).IsEqualTo(outboxId);
        await Assert.That(result.SchedulerTriggered).IsTrue();
        await Assert.That(result.SchedulerFailureCategory).IsEqualTo("none");
    }

    [Test]
    public async Task ScheduleEventReminderStillReturnsDurableOutboxWhenTriggerIsDisabled()
    {
        var repository = Substitute.For<IEmailDispatchOutboxRepository>();
        var scheduler = new EventLifecycleScheduler(
            new EventLifecycleEmailOutboxFactory(),
            repository,
            new NoOpScheduledEmailDispatchTrigger());
        repository.Create(Arg.Any<EmailDispatchOutbox>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var outbox = call.Arg<EmailDispatchOutbox>();
                outbox.Id = Guid.CreateVersion7();
                return outbox;
            });

        var result = await scheduler.ScheduleEventReminderAsync(
            new EventReminderScheduleInput(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "attendee@example.test",
                "Community Dinner",
                DateTimeOffset.UtcNow.AddDays(3),
                DateTimeOffset.UtcNow.AddHours(1)),
            CancellationToken.None);

        await Assert.That(result.SchedulerTriggered).IsFalse();
        await Assert.That(result.SchedulerFailureCategory).IsEqualTo("scheduler_disabled");
        await Assert.That(result.PublishEventId).IsNotEqualTo(Guid.Empty);
    }
}
