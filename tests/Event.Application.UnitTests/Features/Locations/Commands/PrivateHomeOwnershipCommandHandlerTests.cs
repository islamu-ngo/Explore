// ABOUTME: Invariant tests for consent-backed Private Home classification and ownership acceptance.
// ABOUTME: Proves ownership never moves without an explicit versioned acknowledgement by the new owner.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.Locations.Handlers.Commands;
using Explore.Application.Features.Locations.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace ApplicationUnitTests.Features.Locations.Commands;

[Category("EventLocationPrivacy")]
public sealed class PrivateHomeOwnershipCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    [Test]
    [Arguments(false, "private-home-consent/2026-08")]
    [Arguments(true, "")]
    [Arguments(true, "   ")]
    public async Task Classify_WithoutExplicitVersionedConsent_IsRefusedBeforeAnyLoad(
        bool acknowledged,
        string consentVersion)
    {
        var locations = Substitute.For<ILocationRepository>();
        var handler = new ClassifyLocationAsPrivateHomeCommandHandler(
            locations,
            CurrentUser(Guid.CreateVersion7()));

        BaseCommandResponse<Guid> response = await handler.Handle(
            new ClassifyLocationAsPrivateHomeCommand
            {
                LocationId = Guid.CreateVersion7(),
                ExpectedConcurrencyStamp = Guid.CreateVersion7(),
                ConsentAcknowledged = acknowledged,
                ConsentVersion = consentVersion
            },
            CancellationToken.None);

        await Assert.That(response.IsSuccess).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("private_home_consent_required");
        await locations.DidNotReceive().GetById(Arg.Any<Guid>());
        await locations.DidNotReceive().Update(Arg.Any<Location>());
    }

    [Test]
    public async Task Classify_WithConsent_MakesTheActingUserTheOwner()
    {
        Guid actorId = Guid.CreateVersion7();
        Location location = CreateLocation();
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id).Returns(location);
        var handler = new ClassifyLocationAsPrivateHomeCommandHandler(locations, CurrentUser(actorId));

        BaseCommandResponse<Guid> response = await handler.Handle(
            Classify(location, actorId),
            CancellationToken.None);

        await Assert.That(response.IsSuccess).IsTrue();
        await Assert.That(location.LocationKindId).IsEqualTo((int)LocationKindEnum.PrivateHome);
        await Assert.That(location.OwnerUserId).IsEqualTo(actorId);
        await locations.Received(1).Update(location);
    }

    [Test]
    public async Task Classify_WhenAnotherUserAlreadyOwnsTheHome_IsRejectedWithoutMutating()
    {
        Guid ownerId = Guid.CreateVersion7();
        Guid intruderId = Guid.CreateVersion7();
        Location location = CreateLocation();
        location.ClassifyAsPrivateHome(ownerId);
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id).Returns(location);
        var handler = new ClassifyLocationAsPrivateHomeCommandHandler(locations, CurrentUser(intruderId));

        BaseCommandResponse<Guid> response = await handler.Handle(
            Classify(location, intruderId),
            CancellationToken.None);

        await Assert.That(response.IsSuccess).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("private_home_ownership_rejected");
        await Assert.That(location.OwnerUserId).IsEqualTo(ownerId);
        await locations.DidNotReceive().Update(Arg.Any<Location>());
    }

    [Test]
    public async Task Classify_WithAStaleConcurrencyStamp_Conflicts()
    {
        Location location = CreateLocation();
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id).Returns(location);
        var handler = new ClassifyLocationAsPrivateHomeCommandHandler(
            locations,
            CurrentUser(Guid.CreateVersion7()));

        await Assert.That(async () => await handler.Handle(
                new ClassifyLocationAsPrivateHomeCommand
                {
                    LocationId = location.Id,
                    ExpectedConcurrencyStamp = Guid.CreateVersion7(),
                    ConsentAcknowledged = true,
                    ConsentVersion = "private-home-consent/2026-08"
                },
                CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
    }

    [Test]
    public async Task Classify_WhenTheLocationIsMissing_ReportsNotFound()
    {
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(Arg.Any<Guid>()).Returns((Location?)null);
        var handler = new ClassifyLocationAsPrivateHomeCommandHandler(
            locations,
            CurrentUser(Guid.CreateVersion7()));

        BaseCommandResponse<Guid> response = await handler.Handle(
            new ClassifyLocationAsPrivateHomeCommand
            {
                LocationId = Guid.CreateVersion7(),
                ExpectedConcurrencyStamp = Guid.CreateVersion7(),
                ConsentAcknowledged = true,
                ConsentVersion = "private-home-consent/2026-08"
            },
            CancellationToken.None);

        await Assert.That(response.FailureCode).IsEqualTo(FailureCodes.NotFound);
    }

    [Test]
    public async Task AcceptOwnership_RecordsTheIncomingOwnerAsBothConsenterAndOwner()
    {
        Guid previousOwnerId = Guid.CreateVersion7();
        Guid newOwnerId = Guid.CreateVersion7();
        Location location = CreateLocation();
        location.ClassifyAsPrivateHome(previousOwnerId);
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id).Returns(location);
        var handler = new AcceptPrivateHomeOwnershipCommandHandler(
            locations,
            CurrentUser(newOwnerId),
            new FakeTimeProvider(Now));

        BaseCommandResponse<Guid> response = await handler.Handle(
            new AcceptPrivateHomeOwnershipCommand
            {
                LocationId = location.Id,
                ExpectedConcurrencyStamp = location.ConcurrencyStamp,
                ConsentAcknowledged = true,
                ConsentVersion = "private-home-consent/2026-08"
            },
            CancellationToken.None);

        await Assert.That(response.IsSuccess).IsTrue();
        await Assert.That(location.OwnerUserId).IsEqualTo(newOwnerId);
        await Assert.That(location.UpdatedBy).IsEqualTo(newOwnerId);
        await Assert.That(location.UpdatedAt).IsEqualTo(Now.UtcDateTime);
    }

    [Test]
    public async Task AcceptOwnership_OnANonPrivateHome_IsRejected()
    {
        Location location = CreateLocation();
        var locations = Substitute.For<ILocationRepository>();
        locations.GetById(location.Id).Returns(location);
        var handler = new AcceptPrivateHomeOwnershipCommandHandler(
            locations,
            CurrentUser(Guid.CreateVersion7()),
            new FakeTimeProvider(Now));

        BaseCommandResponse<Guid> response = await handler.Handle(
            new AcceptPrivateHomeOwnershipCommand
            {
                LocationId = location.Id,
                ExpectedConcurrencyStamp = location.ConcurrencyStamp,
                ConsentAcknowledged = true,
                ConsentVersion = "private-home-consent/2026-08"
            },
            CancellationToken.None);

        await Assert.That(response.IsSuccess).IsFalse();
        await Assert.That(response.FailureCode).IsEqualTo("private_home_ownership_rejected");
        await locations.DidNotReceive().Update(Arg.Any<Location>());
    }

    [Test]
    public async Task AcceptOwnership_WithoutAnAuthenticatedActor_IsRefused()
    {
        var locations = Substitute.For<ILocationRepository>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);
        var handler = new AcceptPrivateHomeOwnershipCommandHandler(
            locations,
            currentUser,
            new FakeTimeProvider(Now));

        BaseCommandResponse<Guid> response = await handler.Handle(
            new AcceptPrivateHomeOwnershipCommand
            {
                LocationId = Guid.CreateVersion7(),
                ExpectedConcurrencyStamp = Guid.CreateVersion7(),
                ConsentAcknowledged = true,
                ConsentVersion = "private-home-consent/2026-08"
            },
            CancellationToken.None);

        await Assert.That(response.FailureCode).IsEqualTo(FailureCodes.AuthenticationRequired);
        await locations.DidNotReceive().GetById(Arg.Any<Guid>());
    }

    private static ClassifyLocationAsPrivateHomeCommand Classify(Location location, Guid _) => new()
    {
        LocationId = location.Id,
        ExpectedConcurrencyStamp = location.ConcurrencyStamp,
        ConsentAcknowledged = true,
        ConsentVersion = "private-home-consent/2026-08"
    };

    private static ICurrentUserService CurrentUser(Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        return currentUser;
    }

    private static Location CreateLocation()
    {
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = Guid.CreateVersion7(),
            FullName = "Community Centre",
            Country = "BE",
            City = "Brussels",
            ConcurrencyStamp = Guid.CreateVersion7()
        };

        // Address and postcode are created only through the aggregate's atomic manual transition.
        location.SetManualAddress("Rue Neuve 1", "1000");
        return location;
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
