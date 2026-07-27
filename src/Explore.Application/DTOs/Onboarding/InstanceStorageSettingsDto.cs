// ABOUTME: Provider-neutral DTOs for instance-level storage administration.
// ABOUTME: Redacts secrets while exposing provider policy, quotas, usage, and health for admin UI.

using Explore.Application.DTOs.Storage;
using Explore.Application.Models.Storage;
using Explore.Domain;

namespace Explore.Application.DTOs.Onboarding;

public class InstanceStorageSettingsDto
{
    public string Provider { get; set; } = StorageProviders.Local;
    public long DefaultMaxUploadBytes { get; set; } = 10 * 1024 * 1024;
    public long DefaultTenantQuotaBytes { get; set; } = 1024L * 1024 * 1024;
    public long InstanceMaxUploadBytes { get; set; } = 100L * 1024 * 1024;
    public bool LockTenantStorage { get; set; } = true;
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

    public InstanceStorageEffectivePolicyDto EffectivePolicy { get; set; } = new();
    public InstanceStorageUsageDto Usage { get; set; } = new();
    public InstanceStorageProviderStatusDto ProviderStatus { get; set; } = new();
}

public class InstanceStorageEffectivePolicyDto
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

public class InstanceStorageUsageDto
{
    public long UsedBytes { get; set; }
    public long ReservedBytes { get; set; }
    public long QuarantinedBytes { get; set; }
    public long ObjectCount { get; set; }
    public DateTime? LastRecalculatedAt { get; set; }
    public List<InstanceStorageProviderUsageDto> Providers { get; set; } = [];
}

public class InstanceStorageProviderUsageDto
{
    public string Provider { get; set; } = StorageProviders.Local;
    public long UsedBytes { get; set; }
    public long ReservedBytes { get; set; }
    public long QuarantinedBytes { get; set; }
    public long ObjectCount { get; set; }
    public DateTime? LastRecalculatedAt { get; set; }
}

public class InstanceStorageProviderStatusDto
{
    public string Provider { get; set; } = StorageProviders.Local;
    public bool IsAvailable { get; set; }
    public bool SupportsServerSideStreaming { get; set; }
    public bool SupportsBrowserDirectUpload { get; set; }
    public string? FailureCode { get; set; }
    public string? Message { get; set; }
    public S3PreflightResult? Preflight { get; set; }
}
