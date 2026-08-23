// ABOUTME: Verifies bounded, deduplicated EventLocation disclosure orchestration without per-row I/O.
// ABOUTME: Covers public multi-event scope, private identity, room conflicts, and fail-fast limits.

using System.Collections.Immutable;
using System.Reflection;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Tests.Shared.Telemetry;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class EventLocationDisclosureServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 19, 16, 0, 0, TimeSpan.Zero);

    [Test]
    [Category("EventLocationPrivacy")]
    [Category("Todo10EventLocationDisclosure")]
    public async Task ResolveManyAsync_PublicBatchMaySpanEventsAndReturnsImmutableDeduplicatedResults()
    {
        Guid tenantId = Guid.CreateVersion7();
        EventLocation first = CreateTba(tenantId, Guid.CreateVersion7());
        EventLocation second = CreateTba(tenantId, Guid.CreateVersion7());
        var fixture = CreateFixture(authenticatedUserId: null);
        fixture.EventLocations.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([first, second]);
        Guid conflictingRoomA = Guid.CreateVersion7();
        Guid conflictingRoomB = Guid.CreateVersion7();
        EventLocationDisclosureRequest[] requests =
        [
            PublicRequest(first, conflictingRoomA),
            PublicRequest(first, conflictingRoomB),
            PublicRequest(second, null)
        ];

        IReadOnlyDictionary<Guid, EventLocationDisclosureResult> results =
            await fixture.Service.ResolveManyAsync(requests, CancellationToken.None);

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results.Keys).IsEquivalentTo([first.Id, second.Id]);
        await Assert.That(results.Values.All(result =>
            result.Purpose == EventLocationDisclosurePurpose.Public
            && result.State == EventLocationDisclosureState.ToBeAnnounced
            && result.Values is null
            && result.LocationId is null)).IsTrue();
        await Assert.That(results).IsTypeOf<ImmutableDictionary<Guid, EventLocationDisclosureResult>>();
        IDictionary<Guid, EventLocationDisclosureResult> mutableView =
            (IDictionary<Guid, EventLocationDisclosureResult>)results;
        await Assert.ThrowsAsync<NotSupportedException>(() => Task.Run(() => mutableView.Clear()));
        await fixture.EventLocations.Received(1).GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
            Arg.Any<CancellationToken>());
        await fixture.Rooms.DidNotReceive().GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
        await fixture.Governance.Received(1).ResolveAsync(tenantId, Arg.Any<CancellationToken>());
        await fixture.Management.DidNotReceive().AuthorizeManyAsync(
            Arg.Any<IReadOnlyCollection<EventLocation>>(),
            Arg.Any<EventLocationExactReadPurposeEnum>(),
            Arg.Any<Guid?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Category("Todo10EventLocationDisclosure")]
    public async Task ResolveManyAsync_ManagementBatch_PerformsOneReadPerSurfaceAndOneAuthorizationBatch()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid requesterUserId = Guid.CreateVersion7();
        EventLocation first = CreatePhysical(tenantId, eventId, "11 Privacy Lane", "1000");
        EventLocation second = CreatePhysical(tenantId, eventId, "22 Privacy Lane", "2000");
        var firstRoom = new LocationRoom
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            LocationId = first.LocationId!.Value,
            Location = null!,
            Name = "Room A",
            Description = "First room"
        };
        var secondRoom = new LocationRoom
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            LocationId = second.LocationId!.Value,
            Location = null!,
            Name = "Room B",
            Description = "Second room"
        };
        var fixture = CreateFixture(requesterUserId);
        fixture.EventLocations.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([first, second]);
        fixture.Rooms.GetByIdsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([firstRoom, secondRoom]);
        fixture.Management.AuthorizeManyAsync(
                Arg.Any<IReadOnlyCollection<EventLocation>>(),
                EventLocationExactReadPurposeEnum.EventManagement,
                null,
                null,
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, bool>
            {
                [first.Id] = true,
                [second.Id] = false
            });
        EventLocationDisclosureRequest[] requests =
        [
            ManagementRequest(first, firstRoom.Id, requesterUserId),
            ManagementRequest(second, secondRoom.Id, requesterUserId)
        ];

        IReadOnlyDictionary<Guid, EventLocationDisclosureResult> results =
            await fixture.Service.ResolveManyAsync(requests, CancellationToken.None);

        await Assert.That(results[first.Id].State).IsEqualTo(EventLocationDisclosureState.Available);
        await Assert.That(results[first.Id].Values?.StreetAddress).IsEqualTo("11 Privacy Lane");
        await Assert.That(results[first.Id].Values?.RoomName).IsEqualTo("Room A");
        await Assert.That(results[second.Id].State).IsEqualTo(EventLocationDisclosureState.Hidden);
        await Assert.That(results[second.Id].Values).IsNull();
        await fixture.EventLocations.Received(1).GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
            Arg.Any<CancellationToken>());
        await fixture.Rooms.Received(1).GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
            Arg.Any<CancellationToken>());
        await fixture.Governance.Received(1).ResolveAsync(tenantId, Arg.Any<CancellationToken>());
        await fixture.Registrations.DidNotReceive().GetLocationAccessCoverageAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await fixture.Management.Received(1).AuthorizeManyAsync(
            Arg.Is<IReadOnlyCollection<EventLocation>>(items => items.Count == 2),
            EventLocationExactReadPurposeEnum.EventManagement,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Category("Todo10EventLocationDisclosure")]
    public async Task ResolveManyAsync_OverMaximum_RejectsBeforeIdentityDatabaseOrProviderWork()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        EventLocationDisclosureRequest[] requests = Enumerable.Range(
                0,
                IEventLocationDisclosureService.MaximumBatchSize + 1)
            .Select(_ => new EventLocationDisclosureRequest(
                tenantId,
                eventId,
                Guid.CreateVersion7(),
                null,
                null,
                EventLocationDisclosurePurpose.Management))
            .ToArray();
        var fixture = CreateFixture(Guid.CreateVersion7());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            fixture.Service.ResolveManyAsync(requests, CancellationToken.None));

        _ = fixture.CurrentUser.DidNotReceive().IsAuthenticated;
        _ = fixture.CurrentUser.DidNotReceive().UserId;
        await fixture.EventLocations.DidNotReceive().GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
        await fixture.Rooms.DidNotReceive().GetByIdsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
        await fixture.Governance.DidNotReceive().ResolveAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await fixture.Management.DidNotReceive().AuthorizeManyAsync(
            Arg.Any<IReadOnlyCollection<EventLocation>>(),
            Arg.Any<EventLocationExactReadPurposeEnum>(),
            Arg.Any<Guid?>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    private static DisclosureFixture CreateFixture(Guid? authenticatedUserId)
    {
        var eventLocations = Substitute.For<IEventLocationRepository>();
        var rooms = Substitute.For<ILocationRoomRepository>();
        var registrations = Substitute.For<IEventRegistrationRepository>();
        var registrationAccess = Substitute.For<IEventLocationRegistrationAccessService>();
        var governance = Substitute.For<ILocationPrivacyGovernanceService>();
        governance.ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EffectiveLocationPrivacyGovernance(
                true,
                LocationPrivacyGovernanceReasonCode.Resolved,
                true,
                true,
                true,
                LocationDisclosureAudienceEnum.AnyCurrentRegistrant,
                TimeSpan.Zero));
        var management = Substitute.For<IEventLocationManagementAuthorizationService>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(authenticatedUserId.HasValue);
        currentUser.UserId.Returns(authenticatedUserId);
        var service = new EventLocationDisclosureService(
            eventLocations,
            rooms,
            registrations,
            registrationAccess,
            governance,
            management,
            currentUser,
            new EventLocationDisclosureEvaluator(),
            EventLocationPrivacyMetricsFactory.Create(),
            new FixedTimeProvider(Now));
        return new(
            service,
            eventLocations,
            rooms,
            registrations,
            governance,
            management,
            currentUser);
    }

    private static EventLocation CreateTba(Guid tenantId, Guid eventId) =>
        EventLocation.CreateToBeAnnounced(tenantId, eventId, Guid.CreateVersion7(), Now.UtcDateTime);

    private static EventLocation CreatePhysical(
        Guid tenantId,
        Guid eventId,
        string address,
        string postcode)
    {
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            FullName = "Privacy venue",
            Country = "BE",
            City = "Brussels"
        };
        location.ClassifyAs(LocationKindEnum.CommunityVenue);
        location.AttachPii(new LocationPii
        {
            Address = address,
            Postcode = postcode,
            Latitude = 50.85,
            Longitude = 4.35
        });
        EventLocation placement = EventLocation.CreatePhysical(
            tenantId,
            eventId,
            location.Id,
            Guid.CreateVersion7(),
            Now.UtcDateTime);
        placement.ChangeDisclosurePolicy(
            EventLocationDisclosureFields.All,
            LocationDisclosureAudienceEnum.AnyCurrentRegistrant,
            null,
            placement.PolicyVersion,
            Guid.CreateVersion7(),
            EventLocationDisclosureAuditReasonEnum.OrganizerPolicyChange,
            Now.UtcDateTime.AddMinutes(1),
            needsPrivacyReview: false);
        typeof(EventLocation).GetProperty(
                nameof(EventLocation.Location),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(placement, location);
        return placement;
    }

    private static EventLocationDisclosureRequest PublicRequest(EventLocation placement, Guid? roomId) => new(
        placement.TenantId,
        placement.EventId,
        placement.Id,
        roomId,
        null,
        EventLocationDisclosurePurpose.Public);

    private static EventLocationDisclosureRequest ManagementRequest(
        EventLocation placement,
        Guid roomId,
        Guid requesterUserId) => new(
            placement.TenantId,
            placement.EventId,
            placement.Id,
            roomId,
            requesterUserId,
            EventLocationDisclosurePurpose.Management);

    private sealed record DisclosureFixture(
        EventLocationDisclosureService Service,
        IEventLocationRepository EventLocations,
        ILocationRoomRepository Rooms,
        IEventRegistrationRepository Registrations,
        ILocationPrivacyGovernanceService Governance,
        IEventLocationManagementAuthorizationService Management,
        ICurrentUserService CurrentUser);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
