// ABOUTME: Command contract for operator parking of an unsafe EmailDispatch outbox row.
// ABOUTME: Captures tenant scope, target row, audit actor, and bounded reason before durable state mutation.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

[AuthorizeResource(ResourceKinds.EmailDispatch, AuthorizationActions.EmailDispatches.Park)]
public sealed class ParkEmailDispatchCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public Guid OutboxId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ChangedBy { get; set; }

    string? ISecureRequest.ResourceId => OutboxId == Guid.Empty ? null : OutboxId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);
}
