// ABOUTME: Verifies canonical EventLocation placement invariants and fail-closed disclosure defaults.
// ABOUTME: Covers TBA publication, XOR, soft-delete freshness, carrier consistency, and policy identity.

using Explore.Domain.Enums;

namespace Event.Domain.UnitTests;

[Category("EventLocationPrivacy")]
public sealed class EventLocationTests
{
    [Test]
    public async Task PhysicalPlacementHasPrivateLocationIdentityAndFailClosedDefaults()
    {
        var actorId = Guid.CreateVersion7();
        var createdAt = new DateTime(2026, 7, 16, 10, 30, 0, DateTimeKind.Utc);
        var placement = EventLocation.CreatePhysical(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            actorId,
            createdAt);

        await Assert.That(placement.Id.Version).IsEqualTo(7);
        await Assert.That(placement.ConcurrencyStamp.Version).IsEqualTo(7);
        await Assert.That(placement.LocationId).IsNotNull();
        await Assert.That(placement.IsToBeAnnounced).IsFalse();
        await Assert.That(placement.ShowVenueName).IsFalse();
        await Assert.That(placement.ShowCity).IsFalse();
        await Assert.That(placement.ShowCountry).IsFalse();
        await Assert.That(placement.ShowRoomName).IsFalse();
        await Assert.That(placement.ShowStreetAddress).IsFalse();
        await Assert.That(placement.ShowPostcode).IsFalse();
        await Assert.That(placement.ShowCoordinates).IsFalse();
        await Assert.That(placement.FullDetailsAudienceId)
            .IsEqualTo((int)LocationDisclosureAudienceEnum.Never);
        await Assert.That(placement.RevealFullDetailsFromUtc).IsNull();
        await Assert.That(placement.NeedsPrivacyReview).IsTrue();
        await Assert.That(placement.PolicyVersion).IsEqualTo(1);
        await Assert.That(placement.LastPolicyActorUserId).IsEqualTo(actorId);
        await Assert.That(placement.LastPolicyChangedAtUtc).IsEqualTo(createdAt);
        await Assert.That(placement.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(placement.CreatedBy).IsEqualTo(actorId);
        await Assert.That(placement.UpdatedAt).IsNull();
        await Assert.That(placement.UpdatedBy).IsNull();
        await Assert.That(placement.IsDeleted).IsFalse();
        await Assert.That(placement.DeletedAt).IsNull();
        await Assert.That(placement.DeletedBy).IsNull();
    }

    [Test]
    public async Task TenantIdentityCannotBeRetargetedThroughItsInterface()
    {
        var tenantId = Guid.CreateVersion7();
        var placement = EventLocation.CreateToBeAnnounced(
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        await Assert.That(typeof(EventLocation).GetProperty(nameof(EventLocation.TenantId))!.SetMethod!.IsPrivate)
            .IsTrue();
        await Assert.That(() => ((ITenantEntity)placement).TenantId = Guid.CreateVersion7())
            .Throws<InvalidOperationException>();
        await Assert.That(placement.TenantId).IsEqualTo(tenantId);
    }

    [Test]
    public async Task TbaPlacementSatisfiesExclusiveOrWithoutPhysicalLocation()
    {
        var placement = EventLocation.CreateToBeAnnounced(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        await Assert.That(placement.LocationId).IsNull();
        await Assert.That(placement.IsToBeAnnounced).IsTrue();
        await Assert.That(placement.HasValidLocationOrTbaShape).IsTrue();
        await Assert.That(typeof(EventLocation).GetProperty(nameof(EventLocation.IsToBeAnnounced))!.SetMethod!.IsPrivate)
            .IsTrue();
    }

    [Test]
    public async Task ExplicitTbaSatisfiesPublicationWithoutPhysicalDataAndSuppressesEveryField()
    {
        var placement = EventLocation.CreateToBeAnnounced(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        await Assert.That(placement.SatisfiesPublicationVenueRequirement(null)).IsTrue();
        await Assert.That(placement.ShowVenueName).IsFalse();
        await Assert.That(placement.ShowCity).IsFalse();
        await Assert.That(placement.ShowCountry).IsFalse();
        await Assert.That(placement.ShowRoomName).IsFalse();
        await Assert.That(placement.ShowStreetAddress).IsFalse();
        await Assert.That(placement.ShowPostcode).IsFalse();
        await Assert.That(placement.ShowCoordinates).IsFalse();
    }

    [Test]
    public async Task ActiveMatchingPhysicalLocationSatisfiesPublicationVenueRequirement()
    {
        var tenantId = Guid.CreateVersion7();
        var location = CreatePhysicalLocation(tenantId);
        location.AttachPii(new LocationPii { Address = "1 Main Street", Postcode = "1000" });
        var placement = EventLocation.CreatePhysical(
            tenantId,
            Guid.CreateVersion7(),
            location.Id,
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        await Assert.That(placement.SatisfiesPublicationVenueRequirement(location)).IsTrue();
    }

    [Test]
    public async Task EmptyOrWhitespacePhysicalAddressPartsBlockPublication()
    {
        var tenantId = Guid.CreateVersion7();
        var location = CreatePhysicalLocation(tenantId);
        var pii = new LocationPii { Address = string.Empty, Postcode = "1000" };
        location.AttachPii(pii);
        var placement = EventLocation.CreatePhysical(
            tenantId,
            Guid.CreateVersion7(),
            location.Id,
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        await Assert.That(placement.SatisfiesPublicationVenueRequirement(location)).IsFalse();
        pii.Address = "   ";
        await Assert.That(placement.SatisfiesPublicationVenueRequirement(location)).IsFalse();
        pii.Address = "1 Main Street";
        pii.Postcode = string.Empty;
        await Assert.That(placement.SatisfiesPublicationVenueRequirement(location)).IsFalse();
        pii.Postcode = "   ";
        await Assert.That(placement.SatisfiesPublicationVenueRequirement(location)).IsFalse();
    }

    [Test]
    public async Task MissingNotProvidedAndMismatchedPhysicalLocationsBlockPublication()
    {
        var tenantId = Guid.CreateVersion7();
        var location = CreatePhysicalLocation(tenantId);
        var placement = EventLocation.CreatePhysical(
            tenantId,
            Guid.CreateVersion7(),
            location.Id,
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        await Assert.That(placement.SatisfiesPublicationVenueRequirement(null)).IsFalse();
        await Assert.That(placement.SatisfiesPublicationVenueRequirement(location)).IsFalse();

        location.AttachPii(new LocationPii { Address = "1 Main Street", Postcode = "1000" });
        var anotherLocation = CreatePhysicalLocation(tenantId);
        anotherLocation.AttachPii(new LocationPii { Address = "2 Main Street", Postcode = "1000" });
        await Assert.That(placement.SatisfiesPublicationVenueRequirement(anotherLocation)).IsFalse();

        location.TenantId = Guid.CreateVersion7();
        await Assert.That(placement.SatisfiesPublicationVenueRequirement(location)).IsFalse();
    }

    [Test]
    public async Task ErasureBlocksPhysicalPublicationAndNeverInfersTba()
    {
        var tenantId = Guid.CreateVersion7();
        var location = CreatePhysicalLocation(tenantId);
        location.ClassifyAsPrivateHome(Guid.CreateVersion7());
        location.AttachPii(new LocationPii { Address = "9 Household Lane", Postcode = "1000" });
        var placement = EventLocation.CreatePhysical(
            tenantId,
            Guid.CreateVersion7(),
            location.Id,
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        location.EraseOwnedPii(DateTime.UtcNow, LocationPrivacyErasureReasonEnum.AccountDeletion);

        await Assert.That(placement.SatisfiesPublicationVenueRequirement(location)).IsFalse();
        await Assert.That(placement.IsToBeAnnounced).IsFalse();
        await Assert.That(placement.LocationId).IsEqualTo(location.Id);
        await Assert.That(placement.HasValidLocationOrTbaShape).IsTrue();
    }

    [Test]
    public async Task LocationKindNeverChangesDisclosureDefaults()
    {
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            FullName = "Venue",
            Country = "BE",
            City = "Brussels",
            Tenant = null!
        };
        location.ClassifyAsPrivateHome(Guid.CreateVersion7());

        var placement = EventLocation.CreatePhysical(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            location.Id,
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        await Assert.That(location.LocationKindId).IsEqualTo((int)LocationKindEnum.PrivateHome);
        await Assert.That(placement.FullDetailsAudienceId)
            .IsEqualTo((int)LocationDisclosureAudienceEnum.Never);
        await Assert.That(placement.ShowCountry).IsFalse();
    }

    [Test]
    public async Task FinalDetachIsTerminalAndReattachCreatesFreshFailClosedPlacement()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var locationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var first = EventLocation.CreatePhysical(tenantId, eventId, locationId, actorId, DateTime.UtcNow);

        first.DetachFinalReference(actorId, DateTime.UtcNow);
        var replacement = EventLocation.CreatePhysical(tenantId, eventId, locationId, actorId, DateTime.UtcNow);

        await Assert.That(first.IsDeleted).IsTrue();
        await Assert.That(replacement.Id).IsNotEqualTo(first.Id);
        await Assert.That(replacement.IsDeleted).IsFalse();
        await Assert.That(replacement.FullDetailsAudienceId)
            .IsEqualTo((int)LocationDisclosureAudienceEnum.Never);
        await Assert.That(typeof(EventLocation).GetProperty(nameof(EventLocation.LocationId))!.SetMethod!.IsPrivate)
            .IsTrue();
        await Assert.That(() => ((ISoftDeletable)first).IsDeleted = false)
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task SamePhysicalRoomAcrossEventsUsesDistinctEventLocationAuthority()
    {
        var tenantId = Guid.CreateVersion7();
        var locationId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var eventOneId = Guid.CreateVersion7();
        var eventTwoId = Guid.CreateVersion7();
        var placementOne = EventLocation.CreatePhysical(tenantId, eventOneId, locationId, actorId, DateTime.UtcNow);
        var placementTwo = EventLocation.CreatePhysical(tenantId, eventTwoId, locationId, actorId, DateTime.UtcNow);
        var sessionOne = CreateSession(tenantId, eventOneId, roomId);
        var sessionTwo = CreateSession(tenantId, eventTwoId, roomId);
        sessionOne.LocationId = locationId;
        sessionTwo.LocationId = locationId;

        sessionOne.AssignEventLocation(placementOne);
        sessionTwo.AssignEventLocation(placementTwo);

        await Assert.That(sessionOne.LocationId).IsEqualTo(locationId);
        await Assert.That(sessionTwo.LocationId).IsEqualTo(locationId);
        await Assert.That(sessionOne.RoomId).IsEqualTo(roomId);
        await Assert.That(sessionTwo.RoomId).IsEqualTo(roomId);
        await Assert.That(sessionOne.EventLocationId).IsNotEqualTo(sessionTwo.EventLocationId);
    }

    [Test]
    public async Task SwitchingPhysicalLocationsClearsAStaleRoomReference()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var priorLocationId = Guid.CreateVersion7();
        var session = CreateSession(tenantId, eventId, Guid.CreateVersion7());
        session.LocationId = priorLocationId;
        var replacement = EventLocation.CreatePhysical(
            tenantId,
            eventId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        session.AssignEventLocation(replacement);

        await Assert.That(session.LocationId).IsEqualTo(replacement.LocationId);
        await Assert.That(session.RoomId).IsNull();
        await Assert.That(session.Room).IsNull();
    }

    [Test]
    public async Task EveryCarrierDerivesInternalPhysicalIdFromMatchingEventLocation()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var locationId = Guid.CreateVersion7();
        var placement = EventLocation.CreatePhysical(
            tenantId,
            eventId,
            locationId,
            Guid.CreateVersion7(),
            DateTime.UtcNow);
        var session = CreateSession(tenantId, eventId, null);
        var group = new EventSessionGroup { EventId = eventId, Event = null!, TenantId = tenantId, Tenant = null!, Name = "Track" };
        var agenda = new EventAgendaItem { EventId = eventId, Event = null!, TenantId = tenantId, Tenant = null!, Title = "Break" };
        var sessionAgenda = new EventSessionAgendaItem
        {
            EventSessionId = session.Id,
            EventSession = session,
            TenantId = tenantId,
            Tenant = null!,
            Title = "Segment"
        };

        session.AssignEventLocation(placement);
        group.AssignEventLocation(placement);
        agenda.AssignEventLocation(placement);
        sessionAgenda.AssignEventLocation(placement);

        await Assert.That(session.EventLocationId).IsEqualTo(placement.Id);
        await Assert.That(group.EventLocationId).IsEqualTo(placement.Id);
        await Assert.That(agenda.EventLocationId).IsEqualTo(placement.Id);
        await Assert.That(sessionAgenda.EventLocationId).IsEqualTo(placement.Id);
        await Assert.That(session.LocationId).IsEqualTo(locationId);
        await Assert.That(group.LocationId).IsEqualTo(locationId);
        await Assert.That(agenda.LocationId).IsEqualTo(locationId);
        await Assert.That(sessionAgenda.LocationId).IsEqualTo(locationId);
    }

    [Test]
    public async Task CarrierRejectsCrossEventOrCrossTenantPlacement()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var session = CreateSession(tenantId, eventId, null);
        var crossEvent = EventLocation.CreatePhysical(
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTime.UtcNow);
        var crossTenant = EventLocation.CreatePhysical(
            Guid.CreateVersion7(),
            eventId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        await Assert.That(() => session.AssignEventLocation(crossEvent)).Throws<InvalidOperationException>();
        await Assert.That(() => session.AssignEventLocation(crossTenant)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TbaAssignmentClearsLegacyPhysicalRoomKeys()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var session = CreateSession(tenantId, eventId, Guid.CreateVersion7());
        session.LocationId = Guid.CreateVersion7();
        var tba = EventLocation.CreateToBeAnnounced(
            tenantId,
            eventId,
            Guid.CreateVersion7(),
            DateTime.UtcNow);

        session.AssignEventLocation(tba);

        await Assert.That(session.EventLocationId).IsEqualTo(tba.Id);
        await Assert.That(session.LocationId).IsNull();
        await Assert.That(session.RoomId).IsNull();
    }

    private static EventSession CreateSession(Guid tenantId, Guid eventId, Guid? roomId) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        Event = null!,
        TenantId = tenantId,
        Tenant = null!,
        RoomId = roomId
    };

    private static Location CreatePhysicalLocation(Guid tenantId) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        Tenant = null!,
        FullName = "Venue",
        Country = "BE",
        City = "Brussels"
    };
}
