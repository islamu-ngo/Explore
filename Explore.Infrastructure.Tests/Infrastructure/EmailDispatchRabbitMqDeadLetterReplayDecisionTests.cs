// ABOUTME: Unit tests for RabbitMQ EmailDispatch dead-letter replay safety decisions.
// ABOUTME: Verifies DLQ replay validates PostgreSQL truth before replaying or parking pointer messages.

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Infrastructure.Messaging;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class EmailDispatchRabbitMqDeadLetterReplayDecisionTests
{
    [Test]
    public async Task DecideWhenOutboxMissingParksMessage()
    {
        var pointer = CreatePointer();

        var decision = EmailDispatchRabbitMqDeadLetterReplayDecision.Decide(pointer, dispatch: null);

        await Assert.That(decision.Action).IsEqualTo(EmailDispatchRabbitMqDeadLetterReplayAction.Park);
        await Assert.That(decision.FailureCategory).IsEqualTo("outbox_missing");
        await Assert.That(decision.RequiresDurableReplayReset).IsFalse();
    }

    [Test]
    public async Task DecideWhenEventIdMismatchesParksMessage()
    {
        var pointer = CreatePointer(eventId: Guid.CreateVersion7());
        var dispatch = CreateDispatch(pointer, EmailDispatchStatus.DeadLettered);
        dispatch.EventId = Guid.CreateVersion7();

        var decision = EmailDispatchRabbitMqDeadLetterReplayDecision.Decide(pointer, dispatch);

        await Assert.That(decision.Action).IsEqualTo(EmailDispatchRabbitMqDeadLetterReplayAction.Park);
        await Assert.That(decision.FailureCategory).IsEqualTo("event_mismatch");
    }

    [Test]
    public async Task DecideWhenRowAlreadySentParksMessage()
    {
        var pointer = CreatePointer();
        var dispatch = CreateDispatch(pointer, EmailDispatchStatus.Sent);

        var decision = EmailDispatchRabbitMqDeadLetterReplayDecision.Decide(pointer, dispatch);

        await Assert.That(decision.Action).IsEqualTo(EmailDispatchRabbitMqDeadLetterReplayAction.Park);
        await Assert.That(decision.FailureCategory).IsEqualTo("already_sent");
    }

    [Test]
    public async Task DecideWhenRowProcessingParksMessage()
    {
        var pointer = CreatePointer();
        var dispatch = CreateDispatch(pointer, EmailDispatchStatus.Processing);

        var decision = EmailDispatchRabbitMqDeadLetterReplayDecision.Decide(pointer, dispatch);

        await Assert.That(decision.Action).IsEqualTo(EmailDispatchRabbitMqDeadLetterReplayAction.Park);
        await Assert.That(decision.FailureCategory).IsEqualTo("already_processing");
    }

    [Test]
    public async Task DecideWhenRowPendingReplaysWithoutDurableReset()
    {
        var pointer = CreatePointer();
        var dispatch = CreateDispatch(pointer, EmailDispatchStatus.Pending);

        var decision = EmailDispatchRabbitMqDeadLetterReplayDecision.Decide(pointer, dispatch);

        await Assert.That(decision.Action).IsEqualTo(EmailDispatchRabbitMqDeadLetterReplayAction.Replay);
        await Assert.That(decision.RequiresDurableReplayReset).IsFalse();
    }

    [Test]
    public async Task DecideWhenDeferredRowReplaysWithDurableReset()
    {
        var pointer = CreatePointer();
        var dispatch = CreateDispatch(pointer, EmailDispatchStatus.DeadLettered);

        var decision = EmailDispatchRabbitMqDeadLetterReplayDecision.Decide(pointer, dispatch);
        await Assert.That(decision.Action).IsEqualTo(EmailDispatchRabbitMqDeadLetterReplayAction.Replay);
        await Assert.That(decision.RequiresDurableReplayReset).IsTrue();
    }

    private static EmailDispatchPointer CreatePointer(Guid? eventId = null) => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        EmailDispatchKind.RegistrationConfirmation,
        "event_registration_intent",
        Guid.CreateVersion7(),
        eventId,
        RegistrationIntentId: Guid.CreateVersion7(),
        UserId: Guid.CreateVersion7());

    private static EmailDispatchOutbox CreateDispatch(
        EmailDispatchPointer pointer,
        EmailDispatchStatus status) => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = pointer.TenantId,
            PublishEventId = pointer.PublishEventId,
            Kind = pointer.Kind,
            SourceType = pointer.SourceType,
            SourceId = pointer.SourceId ?? Guid.CreateVersion7(),
            EventId = pointer.EventId,
            RegistrationIntentId = pointer.RegistrationIntentId,
            UserId = pointer.UserId,
            RecipientEmail = "attendee@example.test",
            Subject = "Registration confirmation",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
}
