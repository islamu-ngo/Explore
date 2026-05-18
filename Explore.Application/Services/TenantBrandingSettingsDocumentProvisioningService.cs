// ABOUTME: Application service that initializes tenant branding typed settings documents.
// ABOUTME: Creates default tenant.branding JSONB rows idempotently without scalar fallback or dual writes.

namespace Explore.Application.Services;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Domain.Settings.Documents;

public sealed class TenantBrandingSettingsDocumentProvisioningService(
    ITenantRepository tenantRepository,
    ITenantSettingsDocumentRepository tenantSettingsDocumentRepository,
    ITypedSettingsDocumentResolver typedSettingsDocumentResolver)
    : ITenantBrandingSettingsDocumentProvisioningService
{
    public async Task<TenantSettingsDocument> EnsureTenantBrandingDocumentAsync(
        Guid tenantId,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await tenantSettingsDocumentRepository.GetTrackedByTenantAndDocumentKey(
            tenantId,
            SettingsDocumentKeys.Tenant.Branding,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var fallbackDisplayName = displayName;
        if (string.IsNullOrWhiteSpace(fallbackDisplayName))
        {
            var tenant = await tenantRepository.GetById(tenantId);
            fallbackDisplayName = tenant?.FullName;
        }

        var document = TenantBrandingSettingsDocumentDefaults.Create(tenantId, fallbackDisplayName);
        var created = await tenantSettingsDocumentRepository.Create(document);
        typedSettingsDocumentResolver.InvalidateTenantDocumentCache(tenantId, SettingsDocumentKeys.Tenant.Branding);
        return created;
    }
}
