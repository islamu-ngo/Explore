// ABOUTME: Command contract for explicitly resolving replayable email work without sending it.
// ABOUTME: Records a bounded operator reason and transitions the durable row to a resolved skipped state.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

[AuthorizeResource(ResourceKinds.EmailDispatch, AuthorizationActions.EmailDispatches.Resolve)]
public sealed class ResolveEmailDispatchWithoutReplayCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid OutboxId { get; set; }
    public string Reason { get; set; } = string.Empty;
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
