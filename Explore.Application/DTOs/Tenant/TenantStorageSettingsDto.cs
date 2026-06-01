// ABOUTME: Provider-neutral DTOs for tenant-level storage administration.
// ABOUTME: Exposes effective policy, read-only lock state, usage, and redacted optional S3 overrides.

using Explore.Domain;

namespace Explore.Application.DTOs.Tenant;

public class TenantStorageSettingsDto
{
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = StorageProviders.Local;
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
    public long TenantQuotaBytes { get; set; } = 1024L * 1024 * 1024;
    public bool IsReadOnly { get; set; }
    public bool TenantOverridesAllowed { get; set; }
    public bool TenantStorageLocked { get; set; } = true;

    public string S3Endpoint { get; set; } = string.Empty;
    public string S3PublicEndpoint { get; set; } = string.Empty;
    public string S3BucketName { get; set; } = string.Empty;
    public string S3AccessKeyId { get; set; } = string.Empty;
    public string S3SecretAccessKey { get; set; } = string.Empty;
    public bool S3AccessKeyConfigured { get; set; }
    public bool S3SecretAccessKeyConfigured { get; set; }
    public string S3Region { get; set; } = string.Empty;
    public bool S3ForcePathStyle { get; set; } = true;
    public int S3UploadUrlExpirationMinutes { get; set; } = 60;

    public TenantStorageEffectivePolicyDto EffectivePolicy { get; set; } = new();
    public TenantStorageUsageDto Usage { get; set; } = new();
}

public class TenantStorageEffectivePolicyDto
{
    public string Provider { get; set; } = StorageProviders.Local;
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
    public long TenantQuotaBytes { get; set; } = 1024L * 1024 * 1024;
    public long InstanceMaxUploadBytes { get; set; } = 100L * 1024 * 1024;
    public bool TenantOverridesAllowed { get; set; }
    public bool TenantStorageLocked { get; set; } = true;
    public string ProviderSource { get; set; } = "SystemDefault";
    public string MaxUploadSource { get; set; } = "SystemDefault";
    public string QuotaSource { get; set; } = "SystemDefault";
}

public class TenantStorageUsageDto
{
    public string Provider { get; set; } = StorageProviders.Local;
    public long UsedBytes { get; set; }
    public long ReservedBytes { get; set; }
    public long QuarantinedBytes { get; set; }
    public long ObjectCount { get; set; }
    public long AvailableBytes { get; set; }
    public DateTime? LastRecalculatedAt { get; set; }
}
