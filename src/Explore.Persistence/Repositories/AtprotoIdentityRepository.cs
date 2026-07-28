// ABOUTME: Persists global AT Protocol identities keyed by exact DID.
// ABOUTME: Loads the represented Actor so verified metadata refreshes preserve ownership.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AtprotoIdentityRepository(ExploreDbContext dbContext)
    : GenericRepository<AtprotoIdentity, Guid>(dbContext), IAtprotoIdentityRepository
{
    public Task<AtprotoIdentity?> GetByDid(string did, CancellationToken cancellationToken = default) =>
        dbContext.AtprotoIdentities
            .Include(identity => identity.Actor)
                .ThenInclude(actor => actor.ExternalActorSubject)
            .Include(identity => identity.Actor)
                .ThenInclude(actor => actor.Organization)
            .Include(identity => identity.Actor)
                .ThenInclude(actor => actor.Group)
            .SingleOrDefaultAsync(identity => identity.Did == did, cancellationToken);
}
