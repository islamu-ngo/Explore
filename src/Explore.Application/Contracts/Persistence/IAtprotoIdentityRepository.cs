// ABOUTME: Defines global exact-DID identity persistence for Actor federation ownership.
// ABOUTME: Keeps mutable handle and PDS metadata on the credential rather than on Actor.

using Explore.Domain;
using Explore.Domain.ValueObjects;

namespace Explore.Application.Contracts.Persistence;

public interface IAtprotoIdentityRepository : IGenericRepository<AtprotoIdentity, Guid>
{
    Task<AtprotoIdentity?> GetByDid(
        AtprotoDid did,
        CancellationToken cancellationToken = default);
}
