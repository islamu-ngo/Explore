// ABOUTME: Creates a tenant and final branding document inside the caller-owned transaction.
// ABOUTME: Performs no lock acquisition, cache invalidation, notification publication, or nested dispatch.

namespace Explore.Application.Services;

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Domain;
using Explore.Domain.Settings.Documents;

public sealed class TenantCreationService(
    ITenantRepository tenantRepository,
    ITenantSettingsDocumentRepository tenantSettingsDocumentRepository)
    : ITenantCreationService
{
    public async Task<TenantCreationOutcome> CreateInCurrentTransactionAsync(
        TenantCreationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var tenant = new Tenant
        {
            Id = request.TenantId,
            FullName = request.FullName.Trim(),
            Slug = request.Slug.Trim(),
            TenantStatusId = request.TenantStatusId,
            TenantStatus = null!,
            CreatedAt = request.OccurredAt,
            CreatedBy = request.ActorUserId
        };
        Tenant created = await tenantRepository.Create(tenant);
        if (created.Id != request.TenantId)
        {
            throw new InvalidOperationException("Tenant persistence changed the planned aggregate identity.");
        }

        TenantSettingsDocument branding = TenantSettingsDocument.Create(
            request.TenantId,
            request.BrandingDocumentKey,
            request.BrandingSchemaVersion,
            request.BrandingDefaultsVersion,
            request.BrandingPayloadJson);
        branding.Id = request.BrandingDocumentId;
        branding.CreatedAt = request.OccurredAt;
        branding.CreatedBy = request.ActorUserId;
        TenantSettingsDocument createdBranding =
            await tenantSettingsDocumentRepository.Create(branding);
        return new TenantCreationOutcome(created, createdBranding);
    }

    private static void Validate(TenantCreationRequest request)
    {
        if (request.TenantId == Guid.Empty || request.TenantId.Version != 7
            || request.BrandingDocumentId == Guid.Empty
            || request.BrandingDocumentId.Version != 7)
        {
            throw new ArgumentException("Tenant and branding identities must be UUIDv7.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.FullName)
            || string.IsNullOrWhiteSpace(request.Slug)
            || request.TenantStatusId <= 0)
        {
            throw new ArgumentException("Tenant creation identity and status are required.", nameof(request));
        }

        if (request.OccurredAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Tenant creation timestamp must use UTC kind.", nameof(request));
        }

        if (!string.Equals(
                request.BrandingDocumentKey,
                SettingsDocumentKeys.Tenant.Branding,
                StringComparison.Ordinal))
        {
            throw new ArgumentException("Tenant creation requires the canonical branding document.", nameof(request));
        }
    }
}
