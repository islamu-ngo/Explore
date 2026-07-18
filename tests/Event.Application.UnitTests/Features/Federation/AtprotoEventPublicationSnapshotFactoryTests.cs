// ABOUTME: Verifies canonical event snapshot eligibility, tenant isolation, and raw-location fail-closed behavior.
// ABOUTME: Ensures repositories remain entity-first while the Application projection excludes provider and private data.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Federation;

public sealed class AtprotoEventPublicationSnapshotFactoryTests
{
    [Test]
    public async Task CreateAsync_PublicPersistedEvent_BuildsImmutableSnapshot()
    {
        AtprotoEventPublicationEntityGraph graph = CreateGraph();
        AtprotoEventPublicationSnapshotFactory factory = CreateFactory();

        AtprotoEventPublicationSnapshotResult result = await factory.CreateAsync(
            graph,
            new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));

        await Assert.That(result.IsEligible).IsTrue();
        await Assert.That(result.Snapshot!.Name).IsEqualTo("Public event");
        await Assert.That(result.Snapshot.Organizer.DisplayName).IsEqualTo("Organizer");
        await Assert.That(result.Snapshot.ToString()).DoesNotContain("private-provider-canary");
    }

    [Test]
    public async Task CreateAsync_RawLocationWithoutEventLocationAssociation_IsIneligible()
    {
        AtprotoEventPublicationEntityGraph graph = CreateGraph();
        EventSession session = CreateSession(graph.Event);
        session.LocationId = Guid.CreateVersion7();
        graph = graph with { Sessions = [session] };

        AtprotoEventPublicationSnapshotResult result = await CreateFactory().CreateAsync(
            graph,
            new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));

        await Assert.That(result.IsEligible).IsFalse();
        await Assert.That(result.Errors).Contains(error => error.Contains("raw location", StringComparison.Ordinal));
    }

    [Test]
    public async Task CreateAsync_CrossTenantGraphRow_IsIneligible()
    {
        AtprotoEventPublicationEntityGraph graph = CreateGraph();
        EventSession session = CreateSession(graph.Event);
        session.TenantId = Guid.CreateVersion7();
        graph = graph with { Sessions = [session] };

        AtprotoEventPublicationSnapshotResult result = await CreateFactory().CreateAsync(
            graph,
            new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));

        await Assert.That(result.IsEligible).IsFalse();
        await Assert.That(result.Errors).Contains(error => error.Contains("cross-tenant", StringComparison.Ordinal));
    }

    [Test]
    public async Task CreateAsync_LegacyLocationMismatchWithEventLocation_IsIneligible()
    {
        AtprotoEventPublicationEntityGraph graph = CreateGraph();
        var location = new Location
        {
            Id = Guid.CreateVersion7(),
            FullName = "Location",
            Country = "BE",
            City = "Brussels",
            TenantId = graph.Event.TenantId,
            Tenant = null!,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        EventLocation placement = EventLocation.CreatePhysical(
            graph.Event.TenantId,
            graph.Event.Id,
            location.Id,
            Guid.CreateVersion7(),
            DateTime.UtcNow);
        EventSession session = CreateSession(graph.Event);
        session.AssignEventLocation(placement);
        session.LocationId = Guid.CreateVersion7();
        graph = graph with { EventLocations = [placement], Sessions = [session] };

        AtprotoEventPublicationSnapshotResult result = await CreateFactory().CreateAsync(
            graph,
            new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));

        await Assert.That(result.IsEligible).IsFalse();
        await Assert.That(result.Errors).Contains(error => error.Contains("does not match", StringComparison.Ordinal));
    }

    [Test]
    public async Task CreateAsync_StandaloneEventLocation_IsEvaluatedAndIncluded()
    {
        AtprotoEventPublicationEntityGraph graph = CreateGraph();
        EventLocation placement = EventLocation.CreateToBeAnnounced(
            graph.Event.TenantId,
            graph.Event.Id,
            Guid.CreateVersion7(),
            DateTime.UtcNow);
        graph = graph with { EventLocations = [placement] };

        AtprotoEventPublicationSnapshotResult result = await CreateFactory().CreateAsync(
            graph,
            new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));

        await Assert.That(result.IsEligible).IsTrue();
        await Assert.That(result.Snapshot!.Locations).Count().IsEqualTo(1);
        await Assert.That(result.Snapshot.Locations[0].State).IsEqualTo(
            Explore.Application.Contracts.LocationPrivacy.EventLocationDisclosureState.ToBeAnnounced);
    }

    [Test]
    public async Task CreateAsync_PublicSessionWithoutLoadedStatus_IsIneligibleWithoutNumericFallback()
    {
        AtprotoEventPublicationEntityGraph graph = CreateGraph();
        EventSession session = CreateSession(graph.Event);
        session.EventSessionStatus = null!;
        graph = graph with { Sessions = [session] };

        AtprotoEventPublicationSnapshotResult result = await CreateFactory().CreateAsync(
            graph,
            new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));

        await Assert.That(result.IsEligible).IsFalse();
        await Assert.That(result.Errors).Contains(error => error.Contains("status lookup", StringComparison.Ordinal));
    }

    [Test]
    [Arguments(false, VisibilityTypeEnum.Public, false)]
    [Arguments(true, VisibilityTypeEnum.Private, false)]
    [Arguments(true, VisibilityTypeEnum.Public, true)]
    public async Task CreateAsync_NonpublicOrDeletedSeries_IsExcluded(
        bool isPublished,
        VisibilityTypeEnum visibility,
        bool isDeleted)
    {
        AtprotoEventPublicationEntityGraph graph = CreateGraph();
        graph.Event.EventSeries = new Explore.Domain.EventSeries
        {
            Id = Guid.CreateVersion7(),
            Title = "private-series-canary",
            ActorId = graph.Event.ActorId,
            Actor = graph.Event.Actor,
            IsPublished = isPublished,
            VisibilityTypeId = (int)visibility,
            VisibilityType = new VisibilityType
            {
                Id = (int)visibility,
                MasterCode = visibility.ToString().ToUpperInvariant(),
                FullName = visibility.ToString()
            },
            TenantId = graph.Event.TenantId,
            Tenant = null!,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

        AtprotoEventPublicationSnapshotResult result = await CreateFactory().CreateAsync(
            graph,
            new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero));

        await Assert.That(result.IsEligible).IsTrue();
        await Assert.That(result.Snapshot!.Series).IsNull();
        await Assert.That(AtprotoEventDescriptionFormatter.Format(result.Snapshot)).DoesNotContain("private-series-canary");
    }

    private static AtprotoEventPublicationSnapshotFactory CreateFactory()
    {
        ILocationPrivacyGovernanceService governance = Substitute.For<ILocationPrivacyGovernanceService>();
        governance.ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EffectiveLocationPrivacyGovernance(
                true,
                LocationPrivacyGovernanceReasonCode.Resolved,
                true,
                true,
                true,
                LocationDisclosureAudienceEnum.AnyCurrentRegistrant,
                TimeSpan.FromDays(30)));
        return new(new(governance, new EventLocationDisclosureEvaluator()));
    }

    private static AtprotoEventPublicationEntityGraph CreateGraph()
    {
        Guid tenantId = Guid.CreateVersion7();
        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            ActorTypeId = 1,
            ActorType = new ActorType { Id = 1, MasterCode = "ORG", FullName = "Organization" },
            TenantId = tenantId,
            Tenant = null!,
            Pii = new ActorPii { DisplayName = "Organizer", Did = "private-provider-canary" },
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        var eventEntity = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Tenant = null!,
            Title = "Public event",
            ActorId = actor.Id,
            Actor = actor,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = new VisibilityType { Id = (int)VisibilityTypeEnum.Public, MasterCode = "PUBLIC", FullName = "Public" },
            EventStatusId = (int)EventStatusEnum.Draft,
            EventStatus = new EventStatus { Id = (int)EventStatusEnum.Draft, MasterCode = "DRAFT", FullName = "Draft" },
            EventFormatId = (int)EventFormatEnum.Digital,
            EventFormat = new EventFormat { Id = (int)EventFormatEnum.Digital, MasterCode = "DIGITAL", FullName = "Digital" },
            CreatedAt = new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc),
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        return new(eventEntity, [], [], [], [], [], [], [], [], [], [], [], [], [], [], []);
    }

    private static EventSession CreateSession(Explore.Domain.Event eventEntity)
        => new()
        {
            Id = Guid.CreateVersion7(),
            EventId = eventEntity.Id,
            Event = eventEntity,
            TenantId = eventEntity.TenantId,
            Tenant = null!,
            EventSessionStatusId = (int)EventSessionStatusEnum.Published,
            EventSessionStatus = new EventSessionStatus
            {
                Id = (int)EventSessionStatusEnum.Published,
                MasterCode = "PUBLISHED",
                FullName = "Published"
            },
            Title = "Session",
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
}
