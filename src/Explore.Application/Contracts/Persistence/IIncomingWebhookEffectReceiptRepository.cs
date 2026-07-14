// ABOUTME: Repository contract for durable incoming webhook effect receipts.
// ABOUTME: Resolves and creates tenant-scoped idempotency proof without exposing persistence DTOs.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IIncomingWebhookEffectReceiptRepository
{
    Task<IncomingWebhookEffectReceipt?> GetByIdentityAsync(
        Guid tenantId,
        Guid incomingWebhookMessageId,
        string effectKind,
        CancellationToken cancellationToken);

    Task AddAsync(IncomingWebhookEffectReceipt receipt, CancellationToken cancellationToken);
}
