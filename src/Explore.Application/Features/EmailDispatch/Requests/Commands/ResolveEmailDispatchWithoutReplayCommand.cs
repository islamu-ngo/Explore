// ABOUTME: Command contract for explicitly resolving replayable email work without sending it.
// ABOUTME: Records a bounded operator reason and transitions the durable row to a resolved skipped state.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

[AuthorizeResource(ResourceKinds.EmailDispatch, AuthorizationActions.EmailDispatches.Resolve)]
public sealed record ResolveEmailDispatchWithoutReplayCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }
    public Guid OutboxId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public Guid? ChangedBy { get; init; }

    string? ISecureRequest.ResourceId => OutboxId == Guid.Empty ? null : OutboxId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);
}
