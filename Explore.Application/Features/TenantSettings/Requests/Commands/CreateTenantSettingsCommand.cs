// ABOUTME: MediatR command for creating tenant settings.
// ABOUTME: Carries the CreateTenantSettingsDto payload.
using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Requests.Commands;

[AuthorizeResource("tenant_setting", PermissionAction.Create)]
public class CreateTenantSettingsCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required CreateTenantSettingsDto TenantSettingsDto { get; set; }

    string? ISecureRequest.ResourceId => null;
    IDictionary<string, object>? ISecureRequest.ResourceAttributes =>
        TenantSettingsDto.TenantId != Guid.Empty
            ? new Dictionary<string, object> { ["tenantId"] = TenantSettingsDto.TenantId.ToString() }
            : null;
}
