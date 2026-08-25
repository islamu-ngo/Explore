// ABOUTME: Tests explicit operator resolution of replayable email dispatch rows without SMTP delivery.
// ABOUTME: Protects eligible states, redaction fences, audit reasons, and application-owned transactions.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EmailDispatch;
using Explore.Application.Features.EmailDispatch.Handlers.Commands;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EmailDispatch.Commands;

public sealed class ResolveEmailDispatchWithoutReplayCommandHandlerTests
{
    [Test]
    public async Task HandleWhenDeadLetteredTransitionsToSkippedResolution()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        var repository = Substitute.For<IEmailDispatchOutboxRepository>();
        repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchOutbox
            {
                Id = outboxId,
                TenantId = tenantId,
                RecipientEmail = "recipient@example.test",
                Subject = "subject",
                SourceType = "test",
                Status = EmailDispatchStatus.DeadLettered
            });
        repository.TryResolveWithoutReplay(
                tenantId,
                outboxId,
                "Reviewed and closed.",
                Arg.Any<Guid?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new ResolveEmailDispatchWithoutReplayCommandHandler(repository, CreateUnitOfWork());

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ResolveEmailDispatchWithoutReplayCommand
            {
                TenantId = tenantId,
                OutboxId = outboxId,
                Reason = "Reviewed and closed."
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await repository.Received(1).TryResolveWithoutReplay(
            tenantId,
            outboxId,
            "Reviewed and closed.",
            Arg.Any<Guid?>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenContentRedactedRejectsResolution()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        var repository = Substitute.For<IEmailDispatchOutboxRepository>();
        repository.GetByTenantAndId(tenantId, outboxId, Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchOutbox
            {
                Id = outboxId,
                TenantId = tenantId,
                RecipientEmail = string.Empty,
                Subject = string.Empty,
                SourceType = "test",
                Status = EmailDispatchStatus.DeadLettered,
                ContentRedactedAt = DateTime.UtcNow
            });
        var handler = new ResolveEmailDispatchWithoutReplayCommandHandler(repository, CreateUnitOfWork());

        BaseCommandResponse<Guid> result = await handler.Handle(
            new ResolveEmailDispatchWithoutReplayCommand
            {
                TenantId = tenantId,
                OutboxId = outboxId,
                Reason = "Reviewed and closed."
            },
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EmailDispatchFailureCodes.InvalidTransition);
        await repository.DidNotReceiveWithAnyArgs().TryResolveWithoutReplay(default, default, default!, default, default, default);
    }

    private static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>()(call.ArgAt<CancellationToken>(1)));
        return unitOfWork;
    }
}
