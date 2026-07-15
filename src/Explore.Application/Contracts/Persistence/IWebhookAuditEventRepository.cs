// ABOUTME: Append-only persistence boundary for normalized webhook administrative audit evidence.
// ABOUTME: Intentionally exposes no update or delete operations.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IWebhookAuditEventRepository
{
    Task<WebhookAuditEvent> AppendAsync(
        WebhookAuditEvent auditEvent,
        CancellationToken cancellationToken);
}
