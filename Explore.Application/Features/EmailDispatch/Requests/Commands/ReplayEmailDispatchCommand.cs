// ABOUTME: Command contract for operator replay of a deferred EmailDispatch outbox row.
// ABOUTME: Requeues eligible rows by resetting durable PostgreSQL state without touching SMTP or RabbitMQ directly.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

[AuthorizeResource(ResourceKinds.EmailDispatch, AuthorizationActions.EmailDispatches.Replay)]
public sealed class ReplayEmailDispatchCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid OutboxId { get; set; }
    public Guid? ChangedBy { get; set; }

    string? ISecureRequest.ResourceId => OutboxId == Guid.Empty ? null : OutboxId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["outboxId"] = OutboxId.ToString("D")
        };
}
