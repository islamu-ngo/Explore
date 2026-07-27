// ABOUTME: Verifies event provenance and organizer-claim projections from trusted domain state.
// ABOUTME: Keeps public disclosure and authorization-only ownership metadata mapped independently.

using AutoMapper;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.Profiles;
using Explore.Domain;

namespace Event.Application.UnitTests.Profiles;

public sealed class EventOrganizerClaimMappingTests
{
    [Test]
    public async Task EventListMapping_ProjectsProvenanceCode()
    {
#if USE_COMMERCIAL_LUCKYPENNY_LIBS
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<EventMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
#else
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<EventMappingProfile>());
#endif
        var mapper = configuration.CreateMapper();
        var eventEntity = new Explore.Domain.Event
        {
            Title = "Community program",
            Actor = new Actor
            {
                ActorType = new ActorType { MasterCode = "USER", FullName = "User" },
                Pii = new ActorPii { DisplayName = "Reporter" }
            },
            Tenant = new Tenant
            {
                FullName = "Test tenant",
                Slug = "test",
                TenantStatus = new TenantStatus { MasterCode = "ACTIVE", FullName = "Active" }
            },
            VisibilityType = new VisibilityType { MasterCode = "PUBLIC", FullName = "Public" },
            EventStatus = new EventStatus { MasterCode = "PUBLISHED", FullName = "Published" },
            EventFormat = new EventFormat { MasterCode = "LOCAL", FullName = "Local" },
            EventProvenanceType = new EventProvenanceType
            {
                Id = 2,
                MasterCode = "COMMUNITY_REPORTED",
                FullName = "Community reported"
            }
        };

        var dto = mapper.Map<Explore.Application.DTOs.Event.EventListDto>(eventEntity);

        await Assert.That(dto.ProvenanceTypeCode).IsEqualTo("COMMUNITY_REPORTED");
    }

    [Test]
    public async Task EventOrganizerClaimMapping_ProjectsClaimantActorOwnership()
    {
#if USE_COMMERCIAL_LUCKYPENNY_LIBS
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<EventMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
#else
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<EventMappingProfile>());
#endif
        var mapper = configuration.CreateMapper();
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var claimantActorId = Guid.NewGuid();
        var claimantGroupId = Guid.NewGuid();
        var claim = EventOrganizerClaim.CreatePending(
            tenantId,
            eventId,
            claimantActorId,
            "domain-proof",
            "bounded-reference",
            DateTime.UtcNow);
        typeof(EventOrganizerClaim).GetProperty(nameof(EventOrganizerClaim.ClaimantActor))!
            .SetValue(claim, new Actor
            {
                Id = claimantActorId,
                GroupId = claimantGroupId,
                ActorType = null!,
                Pii = new ActorPii { DisplayName = "Claimant group" }
            });

        var dto = mapper.Map<EventOrganizerClaimDto>(claim);

        await Assert.That(dto.ClaimantActorGroupId).IsEqualTo(claimantGroupId);
        await Assert.That(dto.ClaimantActorUserId).IsNull();
        await Assert.That(dto.ClaimantActorOrganizationId).IsNull();
    }
}
