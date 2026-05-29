// ABOUTME: Effective storage policy resolved from instance and tenant governance settings.
// ABOUTME: Captures provider selection, byte ceilings, quota, and delegation lock state for upload flows.

using Explore.Application.Contracts.Infrastructure;

namespace Explore.Application.Models.Storage;

public sealed record ResolvedStoragePolicy(
    Guid? TenantId,
    string Provider,
    long MaxUploadBytes,
    long TenantQuotaBytes,
    long InstanceMaxUploadBytes,
    bool TenantOverridesAllowed,
    bool TenantStorageLocked,
    SettingSource ProviderSource,
    SettingSource MaxUploadSource,
    SettingSource QuotaSource);
