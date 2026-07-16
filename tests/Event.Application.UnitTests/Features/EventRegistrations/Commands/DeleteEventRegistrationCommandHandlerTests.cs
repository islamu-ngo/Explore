// ABOUTME: Unit tests for event registration cancellation command handling.
// ABOUTME: Verifies cancellation delegates to the capacity-aware repository path.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventRegistrations.Handlers.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventRegistrations.Commands;

public sealed class DeleteEventRegistrationCommandHandlerTests
{
    [Test]
    public async Task HandleDelegatesToCapacityAwareCancellationRepositoryMethod()
    {
        var registrationId = Guid.NewGuid();
        var expectedOwnerUserId = Guid.NewGuid();
        var repository = Substitute.For<IEventRegistrationRepository>();
        repository.CancelAndReleaseCapacityAsync(
                registrationId,
                expectedOwnerUserId,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = new DeleteEventRegistrationCommandHandler(repository);

        var result = await handler.Handle(
            new DeleteEventRegistrationCommand
            {
                Id = registrationId,
                ExpectedOwnerUserId = expectedOwnerUserId
            },
            CancellationToken.None);

        await Assert.That(result).IsTrue();
        await repository.Received(1).CancelAndReleaseCapacityAsync(
            registrationId,
            expectedOwnerUserId,
            Arg.Any<CancellationToken>());
        await repository.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    [Test]
    public async Task HandleWithoutPersistedOwnerBindingFailsClosed()
    {
        var repository = Substitute.For<IEventRegistrationRepository>();
        var handler = new DeleteEventRegistrationCommandHandler(repository);

        var result = await handler.Handle(
            new DeleteEventRegistrationCommand { Id = Guid.NewGuid() },
            CancellationToken.None);

        await Assert.That(result).IsFalse();
        await repository.DidNotReceive().CancelAndReleaseCapacityAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
