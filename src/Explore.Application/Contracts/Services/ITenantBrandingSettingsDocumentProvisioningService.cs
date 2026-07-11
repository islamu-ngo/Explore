// ABOUTME: Provisioning contract for tenant branding typed settings document initialization.
// ABOUTME: Guarantees tenant.branding rows exist without scalar fallback or dual writes.

namespace Explore.Application.Contracts.Services;

using Explore.Domain.Settings.Documents;

public interface ITenantBrandingSettingsDocumentProvisioningService
{
    Task<TenantSettingsDocument> EnsureTenantBrandingDocumentAsync(
        Guid tenantId,
        string? displayName = null,
        CancellationToken cancellationToken = default);
}
