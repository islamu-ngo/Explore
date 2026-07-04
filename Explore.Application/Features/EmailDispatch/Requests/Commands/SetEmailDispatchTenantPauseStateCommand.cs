// ABOUTME: Command contract for idempotent tenant-level Basic Dispatch Mode pause and resume controls.
// ABOUTME: Keeps operator write actions in Application while workers read durable PostgreSQL control state.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

[AuthorizeResource(ResourceKinds.EmailDispatch, AuthorizationActions.EmailDispatches.ManageTenant)]
public sealed class SetEmailDispatchTenantPauseStateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; set; }
    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }
    public Guid? ChangedBy { get; set; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["authorizationScope"] = "tenant_control",
            ["emailDispatchOperation"] = IsPaused ? "pause" : "resume"
        };
}
