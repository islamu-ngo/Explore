// ABOUTME: Unit tests for operator EmailDispatch park command handling.
// ABOUTME: Verifies validation, state-machine gating, and repository-only durable transitions.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EmailDispatch;
using Explore.Application.Features.EmailDispatch.Handlers.Commands;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace ApplicationUnitTests.Features.EmailDispatch.Commands;

public sealed class ParkEmailDispatchCommandHandlerTests
{
    private readonly IEmailDispatchOutboxRepository _repository = Substitute.For<IEmailDispatchOutboxRepository>();

    [Test]
    public async Task HandleWhenReasonMissingReturnsValidationFailure()
    {
        var result = await CreateHandler().Handle(
            new ParkEmailDispatchCommand
            {
                TenantId = Guid.NewGuid(),
                OutboxId = Guid.NewGuid(),
                Reason = string.Empty
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors![0]).IsEqualTo("Park reason is required.");
        await _repository.DidNotReceiveWithAnyArgs().TryParkForOperator(default, default, default!, default, default, default);
    }

    [Test]
    public async Task HandleWhenRowMissingReturnsNotFoundFailureCode()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        _repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns((EmailDispatchOutbox?)null);

        var result = await CreateHandler().Handle(
            new ParkEmailDispatchCommand { TenantId = tenantId, OutboxId = outboxId, Reason = "unsafe payload" },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EmailDispatchFailureCodes.NotFound);
        await _repository.DidNotReceiveWithAnyArgs().TryParkForOperator(default, default, default!, default, default, default);
    }

    [Test]
    public async Task HandleWhenRowAlreadySentReturnsInvalidTransition()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        _repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns(CreateOutbox(tenantId, outboxId, EmailDispatchStatus.Sent));

        var result = await CreateHandler().Handle(
            new ParkEmailDispatchCommand { TenantId = tenantId, OutboxId = outboxId, Reason = "unsafe payload" },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EmailDispatchFailureCodes.InvalidTransition);
        await _repository.DidNotReceiveWithAnyArgs().TryParkForOperator(default, default, default!, default, default, default);
    }

    [Test]
    public async Task HandleWhenRowSkippedReturnsInvalidTransition()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        _repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns(CreateOutbox(tenantId, outboxId, EmailDispatchStatus.Skipped));

        var result = await CreateHandler().Handle(
            new ParkEmailDispatchCommand { TenantId = tenantId, OutboxId = outboxId, Reason = "manual review" },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EmailDispatchFailureCodes.InvalidTransition);
        await Assert.That(result.Message).IsEqualTo("Skipped email dispatch rows cannot be parked.");
        await _repository.DidNotReceiveWithAnyArgs().TryParkForOperator(default, default, default!, default, default, default);
    }

    [Test]
    public async Task HandleWhenRowEligibleParksThroughRepository()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        _repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns(CreateOutbox(tenantId, outboxId, EmailDispatchStatus.DeadLettered));
        _repository.TryParkForOperator(
                tenantId,
                outboxId,
                "unsafe payload",
                changedBy,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(
            new ParkEmailDispatchCommand
            {
                TenantId = tenantId,
                OutboxId = outboxId,
                Reason = "unsafe payload",
                ChangedBy = changedBy
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(outboxId);
        await Assert.That(result.Message).IsEqualTo("Email dispatch parked for operator review.");
        await _repository.Received(1).TryParkForOperator(
            tenantId,
            outboxId,
            "unsafe payload",
            changedBy,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    private ParkEmailDispatchCommandHandler CreateHandler() => new(_repository);

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
