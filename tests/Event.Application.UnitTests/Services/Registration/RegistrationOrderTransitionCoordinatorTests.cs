// ABOUTME: Verifies tenant-qualified transition persistence delegates lifecycle authority to the aggregate.
// ABOUTME: Covers missing, stale, illegal, and accepted expected-state writes without repository status commands.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationOrderTransitionCoordinatorTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly IRegistrationInventoryRepository _inventory =
        Substitute.For<IRegistrationInventoryRepository>();

    [Test]
    public async Task PersistAsyncMutatesTheAggregateAndFlushesOnce()
    {
        RegistrationOrder order = CreateOrder();
        _inventory.GetOrderForUpdateWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>())
            .Returns(order);
        var coordinator = new RegistrationOrderTransitionCoordinator(_inventory);

        bool persisted = await coordinator.PersistAsync(
            order.Id,
            _tenantId,
            RegistrationOrderStatusEnum.Draft,
            RegistrationOrderStatusEnum.AwaitingIdentity,
            UtcNow,
            CancellationToken.None);

        await Assert.That(persisted).IsTrue();
        await Assert.That(order.RegistrationOrderStatusId)
            .IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingIdentity);
        await _inventory.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PersistAsyncRejectsMissingStaleAndIllegalTransitionsWithoutFlush()
    {
        RegistrationOrder order = CreateOrder();
        _inventory.GetOrderForUpdateWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>())
            .Returns(order);
        var coordinator = new RegistrationOrderTransitionCoordinator(_inventory);

        bool stale = await coordinator.PersistAsync(
            order.Id,
            _tenantId,
            RegistrationOrderStatusEnum.AwaitingIdentity,
            RegistrationOrderStatusEnum.AwaitingParticipantDetails,
            UtcNow,
            CancellationToken.None);
        bool illegal = await coordinator.PersistAsync(
            order.Id,
            _tenantId,
            RegistrationOrderStatusEnum.Draft,
            RegistrationOrderStatusEnum.Confirmed,
            UtcNow,
            CancellationToken.None);
        bool missing = await coordinator.PersistAsync(
            Guid.CreateVersion7(),
            _tenantId,
            RegistrationOrderStatusEnum.Draft,
            RegistrationOrderStatusEnum.AwaitingIdentity,
            UtcNow,
            CancellationToken.None);

        await Assert.That(stale).IsFalse();
        await Assert.That(illegal).IsFalse();
        await Assert.That(missing).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Draft);
        await _inventory.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private RegistrationOrder CreateOrder() => RegistrationOrder.Create(
        _tenantId,
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        BookingPartyTypeEnum.Individual,
        Guid.CreateVersion7(),
        RegistrationParticipationSnapshot.Create(
            Guid.CreateVersion7(),
            1,
            1,
            1,
            GuestRecoveryPolicyEnum.VerifiedEmailRequired),
        registrationWorkflowVersionId: null,
        CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32])),
        "USD",
        UtcNow,
        UtcNow.AddMinutes(15));
}
