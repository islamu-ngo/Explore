// ABOUTME: Defines the application boundary for the independently retained location-erasure authority.
// ABOUTME: Exposes append and checkpoint-oriented reads only, with no update or delete capability.

using Explore.Domain;

namespace Explore.Application.Contracts.LocationPrivacy;

public interface ILocationPrivacyErasureAuthority
{
    Task<LocationPrivacyErasureAuthorityIntent> AppendAsync(
        LocationPrivacyErasureIntent intent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationPrivacyErasureAuthorityIntent>> ReadAfterAsync(
        long authoritySequence,
        int limit,
        CancellationToken cancellationToken = default);
}
