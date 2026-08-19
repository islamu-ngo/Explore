// ABOUTME: Command contract for a presence-aware patch of the current tenant branding typed settings document.
// ABOUTME: Uses typed JSONB settings only; no scalar fallback, scalar backfill, or dual-write path.

namespace Explore.Application.Features.TenantSettingsDocuments.Requests.Commands;

using Explore.Application.Authorization;
using Explore.Application.DTOs.TenantSettingsDocuments;
using Explore.Application.Responses;
using MediatR;

[AuthorizeResource(ResourceKinds.TenantSetting, AuthorizationActions.Update)]
public sealed class PatchTenantBrandingSettingsDocumentCommand
    : IRequest<BaseCommandResponse<TenantBrandingSettingsDocumentDto>>, ISecureRequest
{
    public required Guid TenantId { get; init; }

    public required PatchTenantBrandingSettingsDocumentDto Patch { get; init; }

    public bool IsLockedByInstance { get; init; }

    string? ISecureRequest.ResourceId => TenantId.ToString();

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new TenantSettingAuthorizationFacts(TenantId, "tenant.branding");
}
