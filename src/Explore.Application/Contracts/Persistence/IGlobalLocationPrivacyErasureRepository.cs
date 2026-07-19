// ABOUTME: Purpose-specific entity repository for global Private Home erasure across tenant boundaries.
// ABOUTME: Loads only owner-bounded Homes, their EventLocation associations, and the owner's actors.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IGlobalLocationPrivacyErasureRepository
{
    Task<IReadOnlyList<Location>> GetOwnedPrivateHomesAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EventLocation>> GetEventLocationsAsync(
        IReadOnlyCollection<Guid> locationIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Actor>> GetUserActorsAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        IReadOnlyCollection<EventLocationDisclosureAudit> audits,
        CancellationToken cancellationToken);
}
