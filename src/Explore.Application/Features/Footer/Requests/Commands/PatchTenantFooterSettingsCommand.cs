// ABOUTME: Command to apply a presence-aware grouped patch to tenant footer scalar settings.
// ABOUTME: Uses trusted tenant authorization metadata and silently skips instance-locked leaves.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Footer;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

[AuthorizeResource(ResourceKinds.Tenant, AuthorizationActions.Update)]
public sealed class PatchTenantFooterSettingsCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public required PatchTenantFooterSettingsDto Patch { get; init; }

    string? ISecureRequest.ResourceId => TenantId == Guid.Empty ? null : TenantId.ToString("D");
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["settingGroup"] = "footer"
        };
}
