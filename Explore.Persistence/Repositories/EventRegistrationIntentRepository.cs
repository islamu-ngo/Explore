// ABOUTME: EF implementation of IEventRegistrationIntentRepository - parent + children inserted inside a serializable transaction.
// ABOUTME: Protects against racing duplicate registrations and keeps the two rows consistent if either write fails.

using System.Data;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventRegistrationIntentRepository : GenericRepository<EventRegistrationIntent, Guid>, IEventRegistrationIntentRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventRegistrationIntentRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventRegistrationIntent?> FindExistingAsync(
        Guid eventId,
        Guid userId,
        int registrationScopeId,
        Guid? selectedEventDayId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EventRegistrationIntents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.EventId == eventId
                    && i.UserId == userId
                    && i.RegistrationScopeId == registrationScopeId
                    && i.SelectedEventDayId == selectedEventDayId,
                cancellationToken);
    }

    public async Task<EventRegistrationIntent> CreateWithChildrenAsync(
        EventRegistrationIntent intent,
        IReadOnlyList<EventRegistration> children,
        CancellationToken cancellationToken)
    {
        await using var tx = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        await _dbContext.EventRegistrationIntents.AddAsync(intent, cancellationToken);

        foreach (var child in children)
        {
            child.EventRegistrationIntentId = intent.Id;
            await _dbContext.EventRegistrations.AddAsync(child, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return intent;
    }
}
