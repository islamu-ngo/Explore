// ABOUTME: Resolves event authority categories from typed provenance and organizer state.
// ABOUTME: Fails closed so listing contributors never inherit organizer or commercial powers.

using Explore.Domain.Enums;

namespace Explore.Domain.Services.Registration;

public static class EventAuthorityRules
{
    public static EventAuthority Resolve(
        int provenanceTypeId,
        Guid publishingActorId,
        Guid? organizerActorId)
    {
        if (publishingActorId == Guid.Empty
            || !Enum.IsDefined(typeof(EventProvenanceTypeEnum), provenanceTypeId))
        {
            return default;
        }

        var hasOrganizerAuthority = organizerActorId is { } actorId && actorId != Guid.Empty;
        return new EventAuthority(
            HasListingAuthority: true,
            HasParticipationManagementAuthority: hasOrganizerAuthority,
            HasDataCollectionAuthority: hasOrganizerAuthority,
            HasCommercialAuthority: hasOrganizerAuthority);
    }
}

public readonly record struct EventAuthority(
    bool HasListingAuthority,
    bool HasParticipationManagementAuthority,
    bool HasDataCollectionAuthority,
    bool HasCommercialAuthority);
