// ABOUTME: Resolves trusted storage upload authorization facts before session creation.
// ABOUTME: Loads owning resources server-side so request-provided owner fields cannot grant authority.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.StorageObjects.Requests.Commands;
using Explore.Domain;

namespace Explore.Application.Features.StorageObjects.Authorization;

public sealed class CreateStorageUploadSessionAuthorizationContextEnricher(
    ICurrentUserService currentUserService,
    ITenantContext tenantContext,
    IOrganizationTenantRepository organizationTenantRepository)
    : IAuthorizationContextEnricher<CreateStorageUploadSessionCommand>
{
    public async Task<AuthorizationContext> ResolveAsync(
        CreateStorageUploadSessionCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var upload = request.UploadSessionDto;
        var attributes = CreateBaseAttributes(tenantId, upload.Purpose, upload.Visibility);

        if (currentUserService.UserId is not { } subjectUserId ||
            tenantId == Guid.Empty ||
            string.IsNullOrWhiteSpace(upload.OwningResourceKind) ||
            upload.OwningResourceId is not { } owningResourceId)
        {
            return new AuthorizationContext(nameof(CreateStorageUploadSessionCommand), attributes);
        }

        if (!string.Equals(upload.OwningResourceKind, StorageOwningResourceKinds.OrganizationTenant, StringComparison.Ordinal))
        {
            return new AuthorizationContext(nameof(CreateStorageUploadSessionCommand), attributes);
        }

        var participation = await organizationTenantRepository.GetById(owningResourceId);
        if (participation is null || participation.TenantId != tenantId)
        {
            return new AuthorizationContext(nameof(CreateStorageUploadSessionCommand), attributes);
        }

        attributes["owningResourceKind"] = StorageOwningResourceKinds.OrganizationTenant;
        attributes["owningResourceId"] = participation.Id.ToString("D");
        attributes["owningOrganizationId"] = participation.OrganizationId.ToString("D");

        return new AuthorizationContext(
            nameof(CreateStorageUploadSessionCommand),
            attributes,
            new StorageUploadIntentFacts(
                subjectUserId,
                participation.TenantId,
                StorageOwningResourceKinds.OrganizationTenant,
                participation.Id,
                participation.OrganizationId));
    }

    private static Dictionary<string, object> CreateBaseAttributes(Guid tenantId, string purpose, string visibility)
    {
        var attributes = new Dictionary<string, object>
        {
            ["purpose"] = purpose,
            ["visibility"] = visibility,
            ["authorizationPhase"] = AuthorizationPhases.PreCreate
        };

        if (tenantId != Guid.Empty)
        {
            attributes["tenantId"] = tenantId.ToString("D");
        }

        return attributes;
    }
}
