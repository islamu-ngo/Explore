// ABOUTME: Command contract for full replacement of the current tenant branding typed settings document.
// ABOUTME: Uses typed JSONB settings only; no scalar fallback, scalar backfill, or dual-write path.

namespace Explore.Application.Features.TenantSettingsDocuments.Requests.Commands;

using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.Responses;
using MediatR;

[AuthorizeResource(ResourceKinds.TenantSetting, AuthorizationActions.Update)]
public sealed class ReplaceTenantBrandingSettingsDocumentCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public required Guid TenantId { get; init; }

    public required ReplaceTenantBrandingSettingsDocumentDto Document { get; init; }

    public bool IsLockedByInstance { get; init; }

    string? ISecureRequest.ResourceId => TenantId.ToString();

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["documentKey"] = "tenant.branding",
        ["isLockedByInstance"] = IsLockedByInstance
    };
}
