// ABOUTME: MediatR command for deleting a tenant settings record by ID.
// ABOUTME: Carries the target record ID.
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.TenantSettings.Requests.Commands;

[AuthorizeResource("tenant_setting", PermissionAction.Delete)]
public class DeleteTenantSettingsCommand : IRequest<bool>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
}
