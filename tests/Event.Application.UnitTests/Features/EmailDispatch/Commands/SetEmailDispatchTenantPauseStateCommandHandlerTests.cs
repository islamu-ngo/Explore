// ABOUTME: Unit tests for Basic Dispatch Mode tenant pause/resume command handling.
// ABOUTME: Verifies idempotent durable-control writes without invoking SMTP or broker transports.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EmailDispatch.Handlers.Commands;
using Explore.Application.Features.EmailDispatch.Requests.Commands;
using Explore.Domain;
using NSubstitute;

namespace ApplicationUnitTests.Features.EmailDispatch.Commands;

public sealed class SetEmailDispatchTenantPauseStateCommandHandlerTests
{
    private readonly IEmailDispatchOutboxRepository _repository = Substitute.For<IEmailDispatchOutboxRepository>();

    [Test]
    public async Task HandleWhenTenantIdMissingReturnsValidationFailure()
    {
        var result = await CreateHandler().Handle(
            new SetEmailDispatchTenantPauseStateCommand { TenantId = Guid.Empty, IsPaused = true },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors![0]).IsEqualTo("TenantId is required.");
        await _repository.DidNotReceiveWithAnyArgs().SetTenantPauseState(default, default, default, default, default, default);
    }

    [Test]
    public async Task HandleWhenPauseReasonTooLongReturnsValidationFailure()
    {
        var result = await CreateHandler().Handle(
            new SetEmailDispatchTenantPauseStateCommand
            {
                TenantId = Guid.NewGuid(),
                IsPaused = true,
                PauseReason = new string('x', 501)
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors![0]).IsEqualTo("Pause reason must be 500 characters or fewer.");
        await _repository.DidNotReceiveWithAnyArgs().SetTenantPauseState(default, default, default, default, default, default);
    }

    [Test]
    public async Task HandleWhenPausingTenantWritesDurablePausedState()
    {
        var tenantId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        var controlId = Guid.NewGuid();

        _repository.SetTenantPauseState(
                tenantId,
                true,
                "incident response",
                changedBy,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchTenantControl { Id = controlId, TenantId = tenantId, IsPaused = true });

        var result = await CreateHandler().Handle(
            new SetEmailDispatchTenantPauseStateCommand
            {
                TenantId = tenantId,
                IsPaused = true,
                PauseReason = "incident response",
                ChangedBy = changedBy
            },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(controlId);
        await Assert.That(result.Message).IsEqualTo("Email dispatch paused for tenant.");
        await _repository.Received(1).SetTenantPauseState(
            tenantId,
            true,
            "incident response",
            changedBy,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenResumingTenantClearsDurablePausedState()
    {
        var tenantId = Guid.NewGuid();
        var controlId = Guid.NewGuid();

        _repository.SetTenantPauseState(
                tenantId,
                false,
                null,
                null,
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(new EmailDispatchTenantControl { Id = controlId, TenantId = tenantId, IsPaused = false });

        var result = await CreateHandler().Handle(
            new SetEmailDispatchTenantPauseStateCommand { TenantId = tenantId, IsPaused = false },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(controlId);
        await Assert.That(result.Message).IsEqualTo("Email dispatch resumed for tenant.");
        await _repository.Received(1).SetTenantPauseState(
            tenantId,
            false,
            null,
            null,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    private SetEmailDispatchTenantPauseStateCommandHandler CreateHandler() => new(_repository);
}
