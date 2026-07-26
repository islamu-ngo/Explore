// ABOUTME: Defines global exact-DID identity persistence for Actor federation ownership.
// ABOUTME: Keeps mutable handle and PDS metadata on the credential rather than on Actor.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IAtprotoIdentityRepository : IGenericRepository<AtprotoIdentity, Guid>
{
    Task<AtprotoIdentity?> GetByDid(string did, CancellationToken cancellationToken = default);
}
