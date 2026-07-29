using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class ActorRepository : GenericRepository<Actor, Guid>, IActorRepository
{
    private readonly ExploreDbContext _dbContext;

    public ActorRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new async Task<Actor?> GetById(Guid id)
    {
        return await _dbContext.Actors
            .Include(a => a.Pii)
            .Include(a => a.ExternalActorSubject)
            .Include(a => a.Organization)
            .Include(a => a.Group)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Actor?> GetActorWithDetails(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .Include(a => a.AtprotoIdentities)
            .Include(a => a.User)
                .ThenInclude(u => u!.Pii)
            .Include(a => a.Organization)
                .ThenInclude(o => o!.Pii)
            .Include(a => a.Group)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Actor?> GetPublicActorProfileAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        PublicActorProfiles()
            .FirstOrDefaultAsync(actor => actor.Id == id, cancellationToken);

    public Task<Actor?> GetLocallyDiscoverableSubscriptionTargetAsync(
        Guid tenantId,
        Guid actorId,
        CancellationToken cancellationToken = default) =>
        PublicActorProfiles()
            .Where(actor => actor.ActorTypeId == (int)ActorTypeEnum.Organization
                || actor.ActorTypeId == (int)ActorTypeEnum.Group)
            .WhereLocallyDiscoverable(_dbContext, tenantId)
            .FirstOrDefaultAsync(actor => actor.Id == actorId, cancellationToken);

    public async Task<Actor?> GetActorByDid(string did)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .Include(a => a.AtprotoIdentities)
            .FirstOrDefaultAsync(a => a.AtprotoIdentities.Any(identity => identity.Did == did));
    }

    public async Task<Actor?> GetActorByHandle(string handle)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .Include(a => a.AtprotoIdentities)
            .FirstOrDefaultAsync(a => a.AtprotoIdentities.Any(identity => identity.Handle == handle));
    }

    public async Task<List<Actor>> GetActorsByTenant(Guid tenantId)
    {
        return await PublicActorProfiles()
            .Include(actor => actor.Organization)
                .ThenInclude(organization => organization!.TenantParticipations.Where(participation =>
                    participation.TenantId == tenantId
                    && participation.ApprovalStatusId == (int)ApprovalStatusEnum.Approved
                    && participation.IsVisible
                    && !participation.IsSuspended
                    && !participation.IsDeleted))
                .ThenInclude(participation => participation.ProfilePicture)
            .Include(actor => actor.Group)
                .ThenInclude(group => group!.TenantParticipations.Where(participation =>
                    participation.TenantId == tenantId
                    && participation.ApprovalStatusId == (int)ApprovalStatusEnum.Approved
                    && participation.IsVisible
                    && !participation.IsSuspended
                    && !participation.IsDeleted))
                .ThenInclude(participation => participation.ProfilePicture)
            .WhereLocallyDiscoverable(_dbContext, tenantId)
            .ToListAsync();
    }

    public async Task<bool> DidExists(string did)
    {
        return await _dbContext.AtprotoIdentities
            .AsNoTracking()
            .AnyAsync(identity => identity.Did == did);
    }

    public async Task<Actor?> GetActorByUserId(Guid userId)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<Actor?> GetTrackedActorByUserId(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Actors
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
    }

    public async Task<Actor?> GetActorByUserIdAndTenantId(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .FirstOrDefaultAsync(a => a.UserId == userId
                && _dbContext.TenantUsers.Any(participation =>
                    participation.TenantId == tenantId && participation.ActorId == a.Id), cancellationToken);
    }

    public async Task<Actor?> GetActorByOrganizationId(Guid organizationId)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .FirstOrDefaultAsync(a => a.OrganizationId == organizationId);
    }

    public async Task<Actor?> GetActorByGroupId(Guid groupId)
    {
        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .FirstOrDefaultAsync(a => a.GroupId == groupId);
    }

    public async Task<(List<Actor> Items, int TotalCount)> GetActorsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.Actors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .Include(a => a.AtprotoIdentities)
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Actor>> SearchAiReferenceActorsAsync(
        string searchTerm,
        int limit,
        CancellationToken cancellationToken)
    {
        string trimmedTerm = searchTerm.Trim();

        if (string.IsNullOrWhiteSpace(trimmedTerm) || limit <= 0)
        {
            return [];
        }

        string pattern = $"%{trimmedTerm}%";

        return await _dbContext.Actors
            .AsNoTracking()
            .Include(a => a.Pii)
            .Include(a => a.ActorType)
            .Include(a => a.AtprotoIdentities)
            .Where(actor => actor.ActorTypeId == (int)ActorTypeEnum.User
                || actor.ActorTypeId == (int)ActorTypeEnum.Organization)
            .Where(actor => actor.Pii != null
                && (EF.Functions.ILike(actor.Pii.DisplayName, pattern)
                    || actor.AtprotoIdentities.Any(identity =>
                        identity.Handle != null && EF.Functions.ILike(identity.Handle, pattern))
                    || (actor.Description != null && EF.Functions.ILike(actor.Description, pattern))))
            .OrderBy(actor => actor.Pii.DisplayName)
            .ThenBy(actor => actor.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ForgetPiiAsync(Guid actorId)
    {
        return await _dbContext.ActorPii
            .Where(p => p.ActorId == actorId)
            .ExecuteDeleteAsync();
    }

    private IQueryable<Actor> PublicActorProfiles() =>
        _dbContext.Actors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(actor => actor.Pii)
            .Include(actor => actor.ActorType)
            .Include(actor => actor.AtprotoIdentities.Where(identity =>
                identity.IsActive
                && !identity.IsSuspended
                && !identity.IsDeleted))
            .Where(actor => !actor.IsDeleted && !actor.IsSuspended);
}
