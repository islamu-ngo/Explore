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

    public override async Task Update(AiConversation entity)
    {
        var entry = _dbContext.Entry(entity);

        if (entry.State != EntityState.Detached)
        {
            _dbContext.ChangeTracker.DetectChanges();

            if (entry.State == EntityState.Modified)
            {
                await PersistAggregateGraphAsync(entity);
                return;
            }

            await _dbContext.SaveChangesAsync();
            return;
        }

        await PersistAggregateGraphAsync(entity);
    }

    private async Task PersistAggregateGraphAsync(AiConversation entity)
    {
        var status = entity.Status;
        var title = entity.Title;
        var provider = entity.Provider;
        var modelId = entity.ModelId;
        var blockedReason = entity.BlockedReason;
        var lastMessageSequence = entity.LastMessageSequence;
        var actorId = entity.ActorId;
        var updatedAt = entity.UpdatedAt;
        var updatedBy = entity.UpdatedBy;
        var concurrencyStamp = Guid.NewGuid();

        var affectedRows = await _dbContext.AiConversations
            .Where(conversation => conversation.Id == entity.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(conversation => conversation.Status, status)
                .SetProperty(conversation => conversation.Title, title)
                .SetProperty(conversation => conversation.Provider, provider)
                .SetProperty(conversation => conversation.ModelId, modelId)
                .SetProperty(conversation => conversation.BlockedReason, blockedReason)
                .SetProperty(conversation => conversation.LastMessageSequence, lastMessageSequence)
                .SetProperty(conversation => conversation.ActorId, actorId)
                .SetProperty(conversation => conversation.UpdatedAt, updatedAt)
                .SetProperty(conversation => conversation.UpdatedBy, updatedBy)
                .SetProperty(conversation => conversation.ConcurrencyStamp, concurrencyStamp));

        if (affectedRows == 0)
        {
            throw new DbUpdateConcurrencyException("The AI conversation aggregate could not be updated.");
        }

        var messages = entity.Messages.ToList();
        var runs = entity.Runs.ToList();
        var references = entity.References.ToList();
        var proposedActions = entity.ProposedActions.ToList();

        foreach (var message in messages)
        {
            message.Conversation = null;
        }

        foreach (var run in runs)
        {
            run.Conversation = null;
        }

        foreach (var reference in references)
        {
            reference.Conversation = null;
        }

        foreach (var action in proposedActions)
        {
            action.Conversation = null;
            action.Message = null;
        }

        _dbContext.ChangeTracker.Clear();

        var messageIds = messages.Select(message => message.Id).ToArray();
        var existingMessageIds = await _dbContext.AiMessages
            .Where(message => messageIds.Contains(message.Id))
            .Select(message => message.Id)
            .ToListAsync();
        _dbContext.AiMessages.AddRange(messages.Where(message => !existingMessageIds.Contains(message.Id)));

        var referenceIds = references.Select(reference => reference.Id).ToArray();
        var existingReferenceIds = await _dbContext.AiConversationReferences
            .Where(reference => referenceIds.Contains(reference.Id))
            .Select(reference => reference.Id)
            .ToListAsync();
        _dbContext.AiConversationReferences.AddRange(references.Where(reference => !existingReferenceIds.Contains(reference.Id)));

        var runIds = runs.Select(run => run.Id).ToArray();
        var existingRunIds = await _dbContext.AiRuns
            .Where(run => runIds.Contains(run.Id))
            .Select(run => run.Id)
            .ToListAsync();
        _dbContext.AiRuns.AddRange(runs.Where(run => !existingRunIds.Contains(run.Id)));
        foreach (var run in runs.Where(run => existingRunIds.Contains(run.Id)))
        {
            await _dbContext.AiRuns
                .Where(existingRun => existingRun.Id == run.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(existingRun => existingRun.Status, run.Status)
                    .SetProperty(existingRun => existingRun.StartedAt, run.StartedAt)
                    .SetProperty(existingRun => existingRun.CompletedAt, run.CompletedAt)
                    .SetProperty(existingRun => existingRun.FailureCode, run.FailureCode)
                    .SetProperty(existingRun => existingRun.FailureMessage, run.FailureMessage));
        }

        var proposedActionIds = proposedActions.Select(action => action.Id).ToArray();
        var existingProposedActionIds = await _dbContext.AiProposedActions
            .Where(action => proposedActionIds.Contains(action.Id))
            .Select(action => action.Id)
            .ToListAsync();
        _dbContext.AiProposedActions.AddRange(proposedActions.Where(action => !existingProposedActionIds.Contains(action.Id)));
        foreach (var action in proposedActions.Where(action => existingProposedActionIds.Contains(action.Id)))
        {
            await _dbContext.AiProposedActions
                .Where(existingAction => existingAction.Id == action.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(existingAction => existingAction.Status, action.Status)
                    .SetProperty(existingAction => existingAction.ConfirmedBy, action.ConfirmedBy)
                    .SetProperty(existingAction => existingAction.ConfirmedAt, action.ConfirmedAt)
                    .SetProperty(existingAction => existingAction.RejectedBy, action.RejectedBy)
                    .SetProperty(existingAction => existingAction.RejectedAt, action.RejectedAt)
                    .SetProperty(existingAction => existingAction.ResultResourceId, action.ResultResourceId)
                    .SetProperty(existingAction => existingAction.FailureCode, action.FailureCode)
                    .SetProperty(existingAction => existingAction.FailureMessage, action.FailureMessage));
        }

        await _dbContext.SaveChangesAsync();

        entity.Status = status;
        entity.Title = title;
        entity.Provider = provider;
        entity.ModelId = modelId;
        entity.BlockedReason = blockedReason;
        entity.LastMessageSequence = lastMessageSequence;
        entity.ActorId = actorId;
        entity.UpdatedAt = updatedAt;
        entity.UpdatedBy = updatedBy;
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

    public async Task<int> CountTenantMessagesSinceAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AiMessages
            .AsNoTracking()
            .Where(message => message.Role == AiMessageRole.User)
            .Where(message => message.CreatedAt >= sinceUtc)
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountRunningConversationsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AiConversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == userId)
            .Where(conversation => conversation.Status == AiConversationStatus.Running)
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
