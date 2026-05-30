// ABOUTME: EF Core repository for AI assistant conversation aggregates and proposed actions.
// ABOUTME: Uses tenant query filters, no-tracking reads, and tracking lookups for state transitions.

using Explore.Application.Contracts.Persistence;
using Explore.Domain.Ai;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class AiConversationRepository : GenericRepository<AiConversation, Guid>, IAiConversationRepository
{
    private readonly ExploreDbContext _dbContext;

    public AiConversationRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AiConversation?> GetByIdWithDetailsAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await IncludeConversationDetails(_dbContext.AiConversations.AsNoTracking())
            .FirstOrDefaultAsync(conversation => conversation.Id == conversationId, cancellationToken);
    }

    public async Task<AiConversation?> GetByIdForUpdateAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await IncludeConversationDetails(_dbContext.AiConversations)
            .FirstOrDefaultAsync(conversation => conversation.Id == conversationId, cancellationToken);
    }

    public async Task<IReadOnlyList<AiConversation>> ListRecentForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return [];
        }

        var boundedLimit = Math.Min(limit, 100);

        return await _dbContext.AiConversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.UpdatedAt ?? conversation.CreatedAt)
            .ThenByDescending(conversation => conversation.Id)
            .Take(boundedLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUserMessagesSinceAsync(
        Guid userId,
        DateTime sinceUtc,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AiMessages
            .AsNoTracking()
            .Where(message => message.Role == AiMessageRole.User)
            .Where(message => message.CreatedBy == userId)
            .Where(message => message.CreatedAt >= sinceUtc)
            .CountAsync(cancellationToken);
    }

    public async Task<AiProposedAction?> GetProposedActionForUpdateAsync(
        Guid proposedActionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AiProposedActions
            .Include(action => action.Conversation)
            .FirstOrDefaultAsync(action => action.Id == proposedActionId, cancellationToken);
    }

    private static IQueryable<AiConversation> IncludeConversationDetails(IQueryable<AiConversation> query)
    {
        return query
            .Include(conversation => conversation.Messages.OrderBy(message => message.Sequence))
            .Include(conversation => conversation.Runs.OrderBy(run => run.QueuedAt))
            .Include(conversation => conversation.References.OrderBy(reference => reference.CreatedAt))
            .Include(conversation => conversation.ProposedActions.OrderBy(action => action.CreatedAt));
    }
}
