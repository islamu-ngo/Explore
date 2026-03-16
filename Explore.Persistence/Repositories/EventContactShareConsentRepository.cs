// ABOUTME: EF Core repository for EventContactShareConsent with scoped lookups.
// ABOUTME: Queries include navigation properties needed for display (Event, Actor with Org/Pii).

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventContactShareConsentRepository : GenericRepository<EventContactShareConsent, Guid>, IEventContactShareConsentRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventContactShareConsentRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventContactShareConsent?> GetByScope(Guid tenantId, Guid userId, Guid recipientActorId, string purposeCode)
    {
        return await _dbContext.EventContactShareConsents
            .FirstOrDefaultAsync(c =>
                c.TenantId == tenantId &&
                c.UserId == userId &&
                c.RecipientActorId == recipientActorId &&
                c.PurposeCode == purposeCode);
    }

    public async Task<(List<EventContactShareConsent> Items, int TotalCount)> GetGrantedForRecipient(
        Guid tenantId, Guid recipientActorId, Guid? eventId, string? emailSearch, int pageNumber, int pageSize)
    {
        var query = _dbContext.EventContactShareConsents
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.SourceEvent)
            .Include(c => c.User)
                .ThenInclude(u => u!.Pii)
            .Where(c => c.TenantId == tenantId &&
                        c.RecipientActorId == recipientActorId &&
                        c.Status == ConsentStatus.Granted);

        if (eventId.HasValue)
            query = query.Where(c => c.SourceEventId == eventId.Value);

        if (!string.IsNullOrWhiteSpace(emailSearch))
            query = query.Where(c => c.EmailNormalizedSnapshot.Contains(emailSearch.ToLowerInvariant()));

        query = query.OrderByDescending(c => c.GrantedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<EventContactShareConsent>> GetByUser(Guid tenantId, Guid userId)
    {
        return await _dbContext.EventContactShareConsents
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.SourceEvent)
            .Include(c => c.RecipientActor)
                .ThenInclude(a => a.Pii)
            .Include(c => c.RecipientActor)
                .ThenInclude(a => a.Organization)
                    .ThenInclude(o => o!.Pii)
            .Where(c => c.TenantId == tenantId && c.UserId == userId)
            .OrderByDescending(c => c.GrantedAt)
            .ToListAsync();
    }

    public async Task<List<EventContactShareConsent>> GetGrantedForExport(Guid tenantId, Guid recipientActorId, Guid? eventId)
    {
        var query = _dbContext.EventContactShareConsents
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.SourceEvent)
            .Include(c => c.RecipientActor)
                .ThenInclude(a => a.Organization)
                    .ThenInclude(o => o!.Pii)
            .Where(c => c.TenantId == tenantId &&
                        c.RecipientActorId == recipientActorId &&
                        c.Status == ConsentStatus.Granted);

        if (eventId.HasValue)
            query = query.Where(c => c.SourceEventId == eventId.Value);

        return await query.OrderByDescending(c => c.GrantedAt).ToListAsync();
    }
}
