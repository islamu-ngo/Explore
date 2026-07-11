// ABOUTME: Unit tests for operator EmailDispatch replay command handling.
// ABOUTME: Verifies only safe deferred states can be reset for durable retry processing.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EmailDispatch;
using Explore.Application.Features.EmailDispatch.Handlers.Commands;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace ApplicationUnitTests.Features.EmailDispatch.Commands;

public sealed class ReplayEmailDispatchCommandHandlerTests
{
    private readonly IEmailDispatchOutboxRepository _repository = Substitute.For<IEmailDispatchOutboxRepository>();

    [Test]
    public async Task HandleWhenOutboxIdMissingReturnsValidationFailure()
    {
        var result = await CreateHandler().Handle(
            new ReplayEmailDispatchCommand { TenantId = Guid.NewGuid(), OutboxId = Guid.Empty },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors![0]).IsEqualTo("OutboxId is required.");
        await _repository.DidNotReceiveWithAnyArgs().TryReplayForOperator(default, default, default, default, default);
    }

    [Test]
    public async Task HandleWhenRowAlreadySentReturnsInvalidTransition()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        _repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns(CreateOutbox(tenantId, outboxId, EmailDispatchStatus.Sent));

        var result = await CreateHandler().Handle(
            new ReplayEmailDispatchCommand { TenantId = tenantId, OutboxId = outboxId },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EmailDispatchFailureCodes.InvalidTransition);
        await _repository.DidNotReceiveWithAnyArgs().TryReplayForOperator(default, default, default, default, default);
    }

    [Test]
    public async Task HandleWhenRowAlreadyPendingReturnsIdempotentSuccess()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        _repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns(CreateOutbox(tenantId, outboxId, EmailDispatchStatus.Pending));

        var result = await CreateHandler().Handle(
            new ReplayEmailDispatchCommand { TenantId = tenantId, OutboxId = outboxId },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Email dispatch is already pending replay.");
        await _repository.DidNotReceiveWithAnyArgs().TryReplayForOperator(default, default, default, default, default);
    }

    [Test]
    public async Task HandleWhenRowSkippedReturnsInvalidTransition()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        _repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns(CreateOutbox(tenantId, outboxId, EmailDispatchStatus.Skipped));

        var result = await CreateHandler().Handle(
            new ReplayEmailDispatchCommand { TenantId = tenantId, OutboxId = outboxId },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EmailDispatchFailureCodes.InvalidTransition);
        await Assert.That(result.Message).IsEqualTo("Skipped email dispatch rows cannot be replayed.");
        await _repository.DidNotReceiveWithAnyArgs().TryReplayForOperator(default, default, default, default, default);
    }

    [Test]
    public async Task HandleWhenDeadLetteredRowReplaysThroughRepository()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        _repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns(CreateOutbox(tenantId, outboxId, EmailDispatchStatus.DeadLettered));
        _repository.TryReplayForOperator(
                tenantId,
                outboxId,
                changedBy,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(
            new ReplayEmailDispatchCommand { TenantId = tenantId, OutboxId = outboxId, ChangedBy = changedBy },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(outboxId);
        await Assert.That(result.Message).IsEqualTo("Email dispatch queued for replay.");
        await _repository.Received(1).TryReplayForOperator(
            tenantId,
            outboxId,
            changedBy,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    private ReplayEmailDispatchCommandHandler CreateHandler() => new(_repository);

    private static EmailDispatchOutbox CreateOutbox(Guid tenantId, Guid outboxId, EmailDispatchStatus status) => new()
    {
        Id = outboxId,
        TenantId = tenantId,
        Status = status,
        SourceType = "event_registration",
        SourceId = Guid.NewGuid(),
        RecipientEmail = "attendee@example.test",
        Subject = "Registration confirmation"
    };
}
