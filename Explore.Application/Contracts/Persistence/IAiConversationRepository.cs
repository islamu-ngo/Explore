// ABOUTME: Repository contract for tenant-scoped AI assistant conversation persistence.
// ABOUTME: Returns domain entities so handlers own DTO mapping, authorization, and HAL shaping.

using Explore.Domain.Ai;
using Explore.Application.Models;

namespace Explore.Application.Contracts.Persistence;

public interface IAiConversationRepository : IGenericRepository<AiConversation, Guid>
{
    Task<AiConversation?> GetByIdWithDetailsAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<AiConversation?> GetByIdForUpdateAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiConversation>> ListRecentForUserAsync(Guid userId, int limit, CancellationToken cancellationToken);
    Task<int> CountUserMessagesSinceAsync(Guid userId, DateTime sinceUtc, CancellationToken cancellationToken);
    Task<int> CountTenantMessagesSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken);
    Task<int> CountRunningConversationsForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<AiProposedAction?> GetProposedActionForUpdateAsync(Guid proposedActionId, CancellationToken cancellationToken);
    Task UpdateProposedActionAsync(AiProposedAction proposedAction, CancellationToken cancellationToken);
    Task CreateToolExecutionAsync(AiToolExecution toolExecution, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiToolExecution>> ListToolExecutionsForProposedActionAsync(
        Guid proposedActionId,
        CancellationToken cancellationToken);
    Task<AiRetentionCleanupResult> RedactExpiredConversationsAsync(
        DateTime cutoffUtc,
        int retentionDays,
        DateTime utcNow,
        bool dryRun,
        CancellationToken cancellationToken);
}
