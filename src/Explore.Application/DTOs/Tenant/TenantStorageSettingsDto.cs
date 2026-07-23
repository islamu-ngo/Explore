// ABOUTME: Provider-neutral DTOs for tenant-level storage administration.
// ABOUTME: Exposes effective policy, read-only lock state, usage, and redacted optional S3 overrides.

using Explore.Application.DTOs.Storage;
using Explore.Application.Models.Common;
using Explore.Domain;

namespace Explore.Application.DTOs.Tenant;

public sealed class PatchTenantStorageSettingsDto
{
    public PatchTenantStoragePolicyDto? Policy { get; set; }
    public PatchTenantStorageS3Dto? S3 { get; set; }
}

public sealed class PatchTenantStoragePolicyDto
{
    public OptionalUpdate<string> Provider { get; set; }
    public OptionalUpdate<long> MaxUploadBytes { get; set; }
    public OptionalUpdate<long> TenantQuotaBytes { get; set; }
    public OptionalUpdate<List<StorageRouteSettingsDto>> Routes { get; set; }
}

public sealed class PatchTenantStorageS3Dto
{
    public OptionalUpdate<string> Endpoint { get; set; }
    public OptionalUpdate<string> PublicEndpoint { get; set; }
    public OptionalUpdate<string> BucketName { get; set; }
    public OptionalUpdate<string> AccessKeyId { get; set; }
    public OptionalUpdate<string> SecretAccessKey { get; set; }
    public OptionalUpdate<string> Region { get; set; }
    public OptionalUpdate<bool> ForcePathStyle { get; set; }
    public OptionalUpdate<int> UploadUrlExpirationMinutes { get; set; }
}

public class TenantStorageSettingsDto
{
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = StorageProviders.Local;
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
    public long TenantQuotaBytes { get; set; } = 1024L * 1024 * 1024;
    public bool IsReadOnly { get; set; }
    public bool TenantOverridesAllowed { get; set; }
    public bool TenantStorageLocked { get; set; } = true;
    public List<StorageRouteSettingsDto> Routes { get; set; } = [];

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
    public List<StorageRouteSettingsDto> Routes { get; set; } = [];
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
