// ABOUTME: Verifies provenance-derived event authority remains separate and fail closed.
// ABOUTME: Protects community contributors from receiving organizer, data, or commercial powers.

using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;

namespace Event.Domain.UnitTests.Services.Registration;

public sealed class EventAuthorityRulesTests
{
    [Test]
    public async Task Resolve_RecognizedPublisherWithoutOrganizer_GrantsListingAuthorityOnly()
    {
        var authority = EventAuthorityRules.Resolve(
            (int)EventProvenanceTypeEnum.CommunityReported,
            Guid.CreateVersion7(),
            organizerActorId: null);

        await Assert.That(authority.HasListingAuthority).IsTrue();
        await Assert.That(authority.HasParticipationManagementAuthority).IsFalse();
        await Assert.That(authority.HasDataCollectionAuthority).IsFalse();
        await Assert.That(authority.HasCommercialAuthority).IsFalse();
    }

    [Test]
    public async Task Resolve_RecognizedPublisherWithOrganizer_GrantsOrganizerAuthorities()
    {
        var authority = EventAuthorityRules.Resolve(
            (int)EventProvenanceTypeEnum.OrganizerCreated,
            Guid.CreateVersion7(),
            Guid.CreateVersion7());

        await Assert.That(authority.HasListingAuthority).IsTrue();
        await Assert.That(authority.HasParticipationManagementAuthority).IsTrue();
        await Assert.That(authority.HasDataCollectionAuthority).IsTrue();
        await Assert.That(authority.HasCommercialAuthority).IsTrue();
    }

    [Test]
    public async Task Resolve_UnknownProvenance_FailsClosed()
    {
        var authority = EventAuthorityRules.Resolve(999, Guid.CreateVersion7(), Guid.CreateVersion7());

        await Assert.That(authority.HasListingAuthority).IsFalse();
        await Assert.That(authority.HasParticipationManagementAuthority).IsFalse();
        await Assert.That(authority.HasDataCollectionAuthority).IsFalse();
        await Assert.That(authority.HasCommercialAuthority).IsFalse();
    }

    [Test]
    public async Task Resolve_EmptyPublishingActor_FailsClosed()
    {
        var authority = EventAuthorityRules.Resolve(
            (int)EventProvenanceTypeEnum.TenantCurated,
            Guid.Empty,
            Guid.CreateVersion7());

        await Assert.That(authority.HasListingAuthority).IsFalse();
        await Assert.That(authority.HasParticipationManagementAuthority).IsFalse();
        await Assert.That(authority.HasDataCollectionAuthority).IsFalse();
        await Assert.That(authority.HasCommercialAuthority).IsFalse();
    }
}
