// ABOUTME: MediatR command for updating tenant settings.
// ABOUTME: Carries the UpdateTenantSettingsDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Requests.Commands;

[AuthorizeResource("tenant_setting", PermissionAction.Update)]
public class UpdateTenantSettingsCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required UpdateTenantSettingsDto TenantSettingsDto { get; set; }

    string? ISecureRequest.ResourceId => TenantSettingsDto.Id.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        TenantSettingsDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = TenantSettingsDto.TenantId.ToString() }
            : null;
}
