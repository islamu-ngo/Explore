// ABOUTME: Verifies irreversible Location privacy lifecycle and consent-backed Private Home ownership.
// ABOUTME: Covers optional PII, erasure tombstones, resurrection rejection, and fresh replacement records.

using Explore.Domain.Enums;

namespace Event.Domain.UnitTests;

[Category("EventLocationPrivacy")]
public sealed class LocationPrivacyLifecycleTests
{
    [Test]
    public async Task LocationWithoutPiiIsNullSafeAndNotProvided()
    {
        var location = CreateLocation();

        await Assert.That(location.Pii).IsNull();
        await Assert.That(location.Address).IsNull();
        await Assert.That(location.Postcode).IsNull();
        await Assert.That(location.Latitude).IsNull();
        await Assert.That(location.Longitude).IsNull();
        await Assert.That(location.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.NotProvided);
    }

    [Test]
    public async Task AttachingPiiTransitionsNotProvidedLocationToActive()
    {
        var location = CreateLocation();

        location.AttachPii(CreatePii("1 Main Street"));

        await Assert.That(location.Pii).IsNotNull();
        await Assert.That(location.Address).IsEqualTo("1 Main Street");
        await Assert.That(location.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.Active);
    }

    [Test]
    public async Task ActivePiiCannotBeSilentlyClearedOrDeclassified()
    {
        var location = CreateLocation();
        location.ClassifyAsPrivateHome(Guid.CreateVersion7());
        location.AttachPii(CreatePii("1 Main Street"));

        await Assert.That(() => location.Pii = null).Throws<InvalidOperationException>();
        await Assert.That(() => location.ClassifyAs(LocationKindEnum.CommercialVenue))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task PrivateHomeDefaultsOwnershipToCurrentUserAndRequiresConsentToTransfer()
    {
        var currentUserId = Guid.CreateVersion7();
        var newOwnerId = Guid.CreateVersion7();
        var location = CreateLocation();
        location.ClassifyAsPrivateHome(currentUserId);

        await Assert.That(location.LocationKindId).IsEqualTo((int)LocationKindEnum.PrivateHome);
        await Assert.That(location.OwnerUserId).IsEqualTo(currentUserId);

        await Assert.That(() => location.TransferPrivateHomeOwnership(
                new LocationOwnershipConsent(newOwnerId, currentUserId, DateTime.UtcNow, "owner-transfer-v1")))
            .Throws<ArgumentException>();

        location.TransferPrivateHomeOwnership(
            new LocationOwnershipConsent(newOwnerId, newOwnerId, DateTime.UtcNow, "owner-transfer-v1"));

        await Assert.That(location.OwnerUserId).IsEqualTo(newOwnerId);
    }

    [Test]
    public async Task ReclassifyingPrivateHomeCannotSilentlyReplaceItsOwner()
    {
        var ownerId = Guid.CreateVersion7();
        var attemptedOwnerId = Guid.CreateVersion7();
        var location = CreateLocation();
        location.ClassifyAsPrivateHome(ownerId);

        location.ClassifyAsPrivateHome(ownerId);

        await Assert.That(location.OwnerUserId).IsEqualTo(ownerId);
        await Assert.That(() => location.ClassifyAsPrivateHome(attemptedOwnerId))
            .Throws<InvalidOperationException>();
        await Assert.That(location.OwnerUserId).IsEqualTo(ownerId);
    }

    [Test]
    public async Task DirectPiiAssignmentRejectsAnotherLocationIdentity()
    {
        var location = CreateLocation();
        var mismatchedPii = CreatePii("1 Main Street");
        mismatchedPii.LocationId = Guid.CreateVersion7();

        await Assert.That(() => location.Pii = mismatchedPii)
            .Throws<InvalidOperationException>();
        await Assert.That(location.Pii).IsNull();
    }

    [Test]
    public async Task NonHomeClassificationNeverRetainsAnOwner()
    {
        var location = CreateLocation();
        location.ClassifyAsPrivateHome(Guid.CreateVersion7());

        location.ClassifyAs(LocationKindEnum.CommercialVenue);

        await Assert.That(location.OwnerUserId).IsNull();
    }

    [Test]
    public async Task EraseOwnedPiiIsIrreversibleAndTombstonesIdentifyingRoomDataUniquely()
    {
        var ownerId = Guid.CreateVersion7();
        var erasedAt = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
        var location = CreateLocation();
        location.ClassifyAsPrivateHome(ownerId);
        location.AttachPii(CreatePii("9 Household Lane"));
        var roomOne = CreateRoom(location, "Amir's office");
        var roomTwo = CreateRoom(location, "Family bedroom");
        location.Rooms.Add(roomOne);
        location.Rooms.Add(roomTwo);

        location.EraseOwnedPii(erasedAt, LocationPrivacyErasureReasonEnum.AccountDeletion);

        await Assert.That(location.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.Erased);
        await Assert.That(location.Pii).IsNull();
        await Assert.That(location.OwnerUserId).IsNull();
        await Assert.That(location.PiiErasedAtUtc).IsEqualTo(erasedAt);
        await Assert.That(location.PiiErasureReason).IsEqualTo(LocationPrivacyErasureReasonEnum.AccountDeletion);
        await Assert.That(location.FullName).IsEqualTo(Location.ErasedPrivateVenueLabel);
        await Assert.That(location.City).IsEqualTo(string.Empty);
        await Assert.That(roomOne.Name).IsNotEqualTo(roomTwo.Name);
        await Assert.That(roomOne.Name).DoesNotContain("Amir");
        await Assert.That(roomTwo.Name).DoesNotContain("Family");
        await Assert.That(roomOne.Description).IsNull();
        await Assert.That(roomTwo.Description).IsNull();
        await Assert.That(roomOne.IsDeleted).IsTrue();
        await Assert.That(roomTwo.IsDeleted).IsTrue();

        await Assert.That(() => location.AttachPii(CreatePii("10 Replacement Lane")))
            .Throws<InvalidOperationException>();
        await Assert.That(() => location.ClassifyAs(LocationKindEnum.CommercialVenue))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ErasedLocationAndRoomRejectDirectPropertyResurrection()
    {
        var location = CreateLocation();
        location.ClassifyAsPrivateHome(Guid.CreateVersion7());
        location.AttachPii(CreatePii("9 Household Lane"));
        var room = CreateRoom(location, "Family bedroom");
        room.Slug = "family-bedroom";
        location.Rooms.Add(room);
        location.EraseOwnedPii(DateTime.UtcNow, LocationPrivacyErasureReasonEnum.AccountDeletion);
        var tombstoneName = room.Name;

        await Assert.That(() => location.FullName = "Resurrected home")
            .Throws<InvalidOperationException>();
        await Assert.That(() => location.Country = "NL")
            .Throws<InvalidOperationException>();
        await Assert.That(() => location.City = "Amsterdam")
            .Throws<InvalidOperationException>();
        await Assert.That(() => room.Name = "Family bedroom")
            .Throws<InvalidOperationException>();
        await Assert.That(() => room.Slug = "family-bedroom")
            .Throws<InvalidOperationException>();
        await Assert.That(() => room.Description = "Restored description")
            .Throws<InvalidOperationException>();
        await Assert.That(() => room.IsDeleted = false)
            .Throws<InvalidOperationException>();

        await Assert.That(location.FullName).IsEqualTo(Location.ErasedPrivateVenueLabel);
        await Assert.That(location.Country).IsEqualTo("BE");
        await Assert.That(location.City).IsEqualTo(string.Empty);
        await Assert.That(room.Name).IsEqualTo(tombstoneName);
        await Assert.That(room.Slug).IsNull();
        await Assert.That(room.Description).IsNull();
        await Assert.That(room.IsDeleted).IsTrue();
    }

    [Test]
    public async Task ReplacementAddressUsesFreshLocationAndFreshRoomRecords()
    {
        var ownerId = Guid.CreateVersion7();
        var erased = CreateLocation();
        erased.ClassifyAsPrivateHome(ownerId);
        erased.AttachPii(CreatePii("Old address"));
        var erasedRoom = CreateRoom(erased, "Study");
        erased.Rooms.Add(erasedRoom);
        erased.EraseOwnedPii(DateTime.UtcNow, LocationPrivacyErasureReasonEnum.OwnerErasureRequest);

        var replacement = CreateLocation();
        replacement.ClassifyAsPrivateHome(ownerId);
        replacement.AttachPii(CreatePii("New address"));
        var replacementRoom = CreateRoom(replacement, "Study");
        replacement.Rooms.Add(replacementRoom);

        await Assert.That(replacement.Id).IsNotEqualTo(erased.Id);
        await Assert.That(replacementRoom.Id).IsNotEqualTo(erasedRoom.Id);
        await Assert.That(replacementRoom.Name).IsEqualTo("Study");
        await Assert.That(replacement.LocationPrivacyStateId)
            .IsEqualTo((int)LocationPrivacyStateEnum.Active);
    }

    private static Location CreateLocation() => new()
    {
        Id = Guid.CreateVersion7(),
        FullName = "Venue",
        Country = "BE",
        City = "Brussels",
        Tenant = null!
    };

    private static LocationPii CreatePii(string address) => new()
    {
        Address = address,
        Postcode = "1000"
    };

    private static LocationRoom CreateRoom(Location location, string name) => new()
    {
        Id = Guid.CreateVersion7(),
        LocationId = location.Id,
        Location = location,
        Name = name,
        Description = $"Description for {name}",
        Tenant = null!
    };
}
