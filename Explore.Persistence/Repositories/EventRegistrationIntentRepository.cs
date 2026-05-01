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

    public async Task<EventRegistrationIntentCreationResult> CreateWithChildrenAndCapacityAsync(
        EventRegistrationIntent intent,
        IReadOnlyList<EventRegistration> children,
        int approvedStatusId,
        int waitlistedStatusId,
        CancellationToken cancellationToken)
    {
        await using var tx = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var waitlistedSessionIds = new List<Guid>();

        foreach (var child in children)
        {
            var reserved = await TryReserveSessionCapacityAsync(child.EventSessionId, cancellationToken);
            child.ApprovalStatusId = reserved ? approvedStatusId : waitlistedStatusId;

            if (!reserved)
            {
                waitlistedSessionIds.Add(child.EventSessionId);
            }
        }

        intent.ApprovalStatusId = waitlistedSessionIds.Count == 0
            ? approvedStatusId
            : waitlistedStatusId;

        await _dbContext.EventRegistrationIntents.AddAsync(intent, cancellationToken);

        foreach (var child in children)
        {
            child.EventRegistrationIntentId = intent.Id;
            await _dbContext.EventRegistrations.AddAsync(child, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return new EventRegistrationIntentCreationResult(intent, waitlistedSessionIds);
    }

    private async Task<bool> TryReserveSessionCapacityAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE event_sessions
            SET current_audience_attendees = COALESCE(current_audience_attendees, 0) + 1
            WHERE id = {sessionId}
              AND is_deleted = false
              AND (
                  max_audience_attendees IS NULL
                  OR COALESCE(current_audience_attendees, 0) < max_audience_attendees
              )
            """, cancellationToken);

        return affectedRows > 0;
    }
}
