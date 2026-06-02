// ABOUTME: UI-facing models for provider-neutral instance and tenant storage administration.
// ABOUTME: Maps regenerated HAL storage DTOs into editable Blazor state and action affordances.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public static class StorageProviderOptions
{
    public const string Local = "local";
    public const string S3Compatible = "s3_compatible";

    public static string Normalize(string? provider) =>
        string.Equals(provider, S3Compatible, StringComparison.OrdinalIgnoreCase)
            ? S3Compatible
            : Local;

    public static string Label(string? provider) =>
        Normalize(provider) == S3Compatible ? "S3-compatible object storage" : "Local file storage";
}

public sealed class InstanceStorageSettingsModel
{
    private const long MiB = 1024L * 1024L;
    private const long GiB = 1024L * 1024L * 1024L;

    public string Provider { get; set; } = StorageProviderOptions.Local;
    public long DefaultMaxUploadBytes { get; set; } = 10 * MiB;
    public long DefaultTenantQuotaBytes { get; set; } = GiB;
    public long InstanceMaxUploadBytes { get; set; } = 100 * MiB;
    public bool LockTenantStorage { get; set; } = true;
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
    public StorageEffectivePolicyModel EffectivePolicy { get; set; } = new();
    public StorageUsageModel Usage { get; set; } = new();
    public StorageProviderStatusModel ProviderStatus { get; set; } = new();
    public bool CanUpdate { get; set; }
    public bool CanTestProvider { get; set; }
    public bool CanRecalculateUsage { get; set; }
    public string? ErrorMessage { get; set; }

    public bool UsesS3CompatibleProvider =>
        string.Equals(Provider, StorageProviderOptions.S3Compatible, StringComparison.OrdinalIgnoreCase);

    public string ProviderLabel => StorageProviderOptions.Label(Provider);

    public long DefaultMaxUploadMiB
    {
        get => BytesToMiB(DefaultMaxUploadBytes);
        set => DefaultMaxUploadBytes = MiBToBytes(value);
    }

    public long InstanceMaxUploadMiB
    {
        get => BytesToMiB(InstanceMaxUploadBytes);
        set => InstanceMaxUploadBytes = MiBToBytes(value);
    }

    public long DefaultTenantQuotaGiB
    {
        get => BytesToGiB(DefaultTenantQuotaBytes);
        set => DefaultTenantQuotaBytes = GiBToBytes(value);
    }

    public static InstanceStorageSettingsModel Failed(string message) => new()
    {
        ErrorMessage = message
    };

    public static InstanceStorageSettingsModel FromHal(HalResourceOfInstanceStorageSettingsDto resource) => new()
    {
        Provider = StorageProviderOptions.Normalize(resource.Provider),
        DefaultMaxUploadBytes = PositiveOrDefault(resource.DefaultMaxUploadBytes, 10 * MiB),
        DefaultTenantQuotaBytes = PositiveOrDefault(resource.DefaultTenantQuotaBytes, GiB),
        InstanceMaxUploadBytes = PositiveOrDefault(resource.InstanceMaxUploadBytes, 100 * MiB),
        LockTenantStorage = resource.LockTenantStorage ?? true,
        S3Endpoint = resource.S3Endpoint ?? string.Empty,
        S3PublicEndpoint = resource.S3PublicEndpoint ?? string.Empty,
        S3BucketName = resource.S3BucketName ?? string.Empty,
        S3AccessKeyId = resource.S3AccessKeyId ?? string.Empty,
        S3SecretAccessKey = resource.S3SecretAccessKey ?? string.Empty,
        S3AccessKeyConfigured = resource.S3AccessKeyConfigured ?? false,
        S3SecretAccessKeyConfigured = resource.S3SecretAccessKeyConfigured ?? false,
        S3Region = resource.S3Region ?? string.Empty,
        S3ForcePathStyle = resource.S3ForcePathStyle ?? true,
        S3UploadUrlExpirationMinutes = PositiveOrDefault(resource.S3UploadUrlExpirationMinutes, 60),
        EffectivePolicy = StorageEffectivePolicyModel.FromHal(resource.EffectivePolicy),
        Usage = StorageUsageModel.FromHal(resource.Usage),
        ProviderStatus = StorageProviderStatusModel.FromHal(resource.ProviderStatus),
        CanUpdate = HasLink(resource._links, "edit"),
        CanTestProvider = HasLink(resource._links, "provider-test"),
        CanRecalculateUsage = HasLink(resource._links, "recalculate-usage")
    };

    public InstanceStorageSettingsDto ToDto() => new()
    {
        Provider = StorageProviderOptions.Normalize(Provider),
        DefaultMaxUploadBytes = DefaultMaxUploadBytes,
        DefaultTenantQuotaBytes = DefaultTenantQuotaBytes,
        InstanceMaxUploadBytes = InstanceMaxUploadBytes,
        LockTenantStorage = LockTenantStorage,
        S3Endpoint = NullIfWhiteSpace(S3Endpoint),
        S3PublicEndpoint = NullIfWhiteSpace(S3PublicEndpoint),
        S3BucketName = NullIfWhiteSpace(S3BucketName),
        S3AccessKeyId = NullIfWhiteSpace(S3AccessKeyId),
        S3SecretAccessKey = NullIfWhiteSpace(S3SecretAccessKey),
        S3AccessKeyConfigured = S3AccessKeyConfigured,
        S3SecretAccessKeyConfigured = S3SecretAccessKeyConfigured,
        S3Region = NullIfWhiteSpace(S3Region),
        S3ForcePathStyle = S3ForcePathStyle,
        S3UploadUrlExpirationMinutes = S3UploadUrlExpirationMinutes
    };

    private static bool HasLink<TLink>(IDictionary<string, TLink>? links, string rel) =>
        links?.ContainsKey(rel) == true;

    private static long PositiveOrDefault(long? value, long fallback) =>
        value is > 0 ? value.Value : fallback;

    private static int PositiveOrDefault(int? value, int fallback) =>
        value is > 0 ? value.Value : fallback;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static long BytesToMiB(long bytes) => Math.Max(1, bytes / MiB);
    private static long MiBToBytes(long mib) => Math.Max(1, mib) * MiB;
    private static long BytesToGiB(long bytes) => Math.Max(1, bytes / GiB);
    private static long GiBToBytes(long gib) => Math.Max(1, gib) * GiB;
}

public sealed class TenantStorageSettingsModel
{
    private const long MiB = 1024L * 1024L;
    private const long GiB = 1024L * 1024L * 1024L;

    public Guid TenantId { get; set; }
    public string Provider { get; set; } = StorageProviderOptions.Local;
    public long MaxUploadBytes { get; set; } = 10 * MiB;
    public long TenantQuotaBytes { get; set; } = GiB;
    public bool IsReadOnly { get; set; } = true;
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
    public StorageEffectivePolicyModel EffectivePolicy { get; set; } = new();
    public StorageUsageModel Usage { get; set; } = new();
    public bool CanUpdate { get; set; }
    public string? ErrorMessage { get; set; }

    public bool UsesS3CompatibleProvider =>
        string.Equals(Provider, StorageProviderOptions.S3Compatible, StringComparison.OrdinalIgnoreCase);

    public string ProviderLabel => StorageProviderOptions.Label(Provider);

    public long MaxUploadMiB
    {
        get => BytesToMiB(MaxUploadBytes);
        set => MaxUploadBytes = MiBToBytes(value);
    }

    public long TenantQuotaGiB
    {
        get => BytesToGiB(TenantQuotaBytes);
        set => TenantQuotaBytes = GiBToBytes(value);
    }

    public bool IsEditable => CanUpdate && !IsReadOnly && !TenantStorageLocked && TenantOverridesAllowed;

    public static TenantStorageSettingsModel Failed(string message) => new()
    {
        ErrorMessage = message,
        IsReadOnly = true
    };

    public static TenantStorageSettingsModel FromHal(HalResourceOfTenantStorageSettingsDto resource) => new()
    {
        TenantId = resource.TenantId ?? Guid.Empty,
        Provider = StorageProviderOptions.Normalize(resource.Provider),
        MaxUploadBytes = PositiveOrDefault(resource.MaxUploadBytes, 10 * MiB),
        TenantQuotaBytes = PositiveOrDefault(resource.TenantQuotaBytes, GiB),
        IsReadOnly = resource.IsReadOnly ?? true,
        TenantOverridesAllowed = resource.TenantOverridesAllowed ?? false,
        TenantStorageLocked = resource.TenantStorageLocked ?? true,
        S3Endpoint = resource.S3Endpoint ?? string.Empty,
        S3PublicEndpoint = resource.S3PublicEndpoint ?? string.Empty,
        S3BucketName = resource.S3BucketName ?? string.Empty,
        S3AccessKeyId = resource.S3AccessKeyId ?? string.Empty,
        S3SecretAccessKey = resource.S3SecretAccessKey ?? string.Empty,
        S3AccessKeyConfigured = resource.S3AccessKeyConfigured ?? false,
        S3SecretAccessKeyConfigured = resource.S3SecretAccessKeyConfigured ?? false,
        S3Region = resource.S3Region ?? string.Empty,
        S3ForcePathStyle = resource.S3ForcePathStyle ?? true,
        S3UploadUrlExpirationMinutes = PositiveOrDefault(resource.S3UploadUrlExpirationMinutes, 60),
        EffectivePolicy = StorageEffectivePolicyModel.FromHal(resource.EffectivePolicy),
        Usage = StorageUsageModel.FromHal(resource.Usage),
        CanUpdate = HasLink(resource._links, "edit")
    };

    public TenantStorageSettingsDto ToDto() => new()
    {
        TenantId = TenantId,
        Provider = StorageProviderOptions.Normalize(Provider),
        MaxUploadBytes = MaxUploadBytes,
        TenantQuotaBytes = TenantQuotaBytes,
        IsReadOnly = IsReadOnly,
        TenantOverridesAllowed = TenantOverridesAllowed,
        TenantStorageLocked = TenantStorageLocked,
        S3Endpoint = NullIfWhiteSpace(S3Endpoint),
        S3PublicEndpoint = NullIfWhiteSpace(S3PublicEndpoint),
        S3BucketName = NullIfWhiteSpace(S3BucketName),
        S3AccessKeyId = NullIfWhiteSpace(S3AccessKeyId),
        S3SecretAccessKey = NullIfWhiteSpace(S3SecretAccessKey),
        S3AccessKeyConfigured = S3AccessKeyConfigured,
        S3SecretAccessKeyConfigured = S3SecretAccessKeyConfigured,
        S3Region = NullIfWhiteSpace(S3Region),
        S3ForcePathStyle = S3ForcePathStyle,
        S3UploadUrlExpirationMinutes = S3UploadUrlExpirationMinutes
    };

    private static bool HasLink<TLink>(IDictionary<string, TLink>? links, string rel) =>
        links?.ContainsKey(rel) == true;

    private static long PositiveOrDefault(long? value, long fallback) =>
        value is > 0 ? value.Value : fallback;

    private static int PositiveOrDefault(int? value, int fallback) =>
        value is > 0 ? value.Value : fallback;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static long BytesToMiB(long bytes) => Math.Max(1, bytes / MiB);
    private static long MiBToBytes(long mib) => Math.Max(1, mib) * MiB;
    private static long BytesToGiB(long bytes) => Math.Max(1, bytes / GiB);
    private static long GiBToBytes(long gib) => Math.Max(1, gib) * GiB;
}

public sealed class StorageEffectivePolicyModel
{
    public string Provider { get; set; } = StorageProviderOptions.Local;
    public long MaxUploadBytes { get; set; }
    public long TenantQuotaBytes { get; set; }
    public long InstanceMaxUploadBytes { get; set; }
    public bool TenantOverridesAllowed { get; set; }
    public bool TenantStorageLocked { get; set; } = true;
    public string ProviderSource { get; set; } = "SystemDefault";
    public string MaxUploadSource { get; set; } = "SystemDefault";
    public string QuotaSource { get; set; } = "SystemDefault";

    public static StorageEffectivePolicyModel FromHal(EffectivePolicy? policy) => new()
    {
        Provider = StorageProviderOptions.Normalize(policy?.Provider),
        MaxUploadBytes = policy?.MaxUploadBytes ?? 0,
        TenantQuotaBytes = policy?.TenantQuotaBytes ?? 0,
        InstanceMaxUploadBytes = policy?.InstanceMaxUploadBytes ?? 0,
        TenantOverridesAllowed = policy?.TenantOverridesAllowed ?? false,
        TenantStorageLocked = policy?.TenantStorageLocked ?? true,
        ProviderSource = policy?.ProviderSource ?? "SystemDefault",
        MaxUploadSource = policy?.MaxUploadSource ?? "SystemDefault",
        QuotaSource = policy?.QuotaSource ?? "SystemDefault"
    };

    public static StorageEffectivePolicyModel FromHal(EffectivePolicy2? policy) => new()
    {
        Provider = StorageProviderOptions.Normalize(policy?.Provider),
        MaxUploadBytes = policy?.MaxUploadBytes ?? 0,
        TenantQuotaBytes = policy?.TenantQuotaBytes ?? 0,
        InstanceMaxUploadBytes = policy?.InstanceMaxUploadBytes ?? 0,
        TenantOverridesAllowed = policy?.TenantOverridesAllowed ?? false,
        TenantStorageLocked = policy?.TenantStorageLocked ?? true,
        ProviderSource = policy?.ProviderSource ?? "SystemDefault",
        MaxUploadSource = policy?.MaxUploadSource ?? "SystemDefault",
        QuotaSource = policy?.QuotaSource ?? "SystemDefault"
    };
}

public sealed class StorageUsageModel
{
    public string? Provider { get; set; }
    public long UsedBytes { get; set; }
    public long ReservedBytes { get; set; }
    public long QuarantinedBytes { get; set; }
    public long ObjectCount { get; set; }
    public long? AvailableBytes { get; set; }
    public DateTimeOffset? LastRecalculatedAt { get; set; }
    public IReadOnlyList<StorageProviderUsageModel> Providers { get; set; } = [];

    public long TotalBytes => UsedBytes + ReservedBytes + QuarantinedBytes;

    public static StorageUsageModel FromHal(Usage? usage) => new()
    {
        UsedBytes = usage?.UsedBytes ?? 0,
        ReservedBytes = usage?.ReservedBytes ?? 0,
        QuarantinedBytes = usage?.QuarantinedBytes ?? 0,
        ObjectCount = usage?.ObjectCount ?? 0,
        LastRecalculatedAt = usage?.LastRecalculatedAt,
        Providers = usage?.Providers?.Select(StorageProviderUsageModel.FromHal).ToList() ?? []
    };

    public static StorageUsageModel FromHal(Usage2? usage) => new()
    {
        Provider = usage?.Provider,
        UsedBytes = usage?.UsedBytes ?? 0,
        ReservedBytes = usage?.ReservedBytes ?? 0,
        QuarantinedBytes = usage?.QuarantinedBytes ?? 0,
        ObjectCount = usage?.ObjectCount ?? 0,
        AvailableBytes = usage?.AvailableBytes,
        LastRecalculatedAt = usage?.LastRecalculatedAt
    };

    public static StorageUsageModel FromDto(InstanceStorageUsageDto? usage) => new()
    {
        UsedBytes = usage?.UsedBytes ?? 0,
        ReservedBytes = usage?.ReservedBytes ?? 0,
        QuarantinedBytes = usage?.QuarantinedBytes ?? 0,
        ObjectCount = usage?.ObjectCount ?? 0,
        LastRecalculatedAt = usage?.LastRecalculatedAt,
        Providers = usage?.Providers?.Select(StorageProviderUsageModel.FromDto).ToList() ?? []
    };
}

public sealed class StorageProviderUsageModel
{
    public string Provider { get; set; } = StorageProviderOptions.Local;
    public long UsedBytes { get; set; }
    public long ReservedBytes { get; set; }
    public long QuarantinedBytes { get; set; }
    public long ObjectCount { get; set; }
    public DateTimeOffset? LastRecalculatedAt { get; set; }
    public long TotalBytes => UsedBytes + ReservedBytes + QuarantinedBytes;

    public static StorageProviderUsageModel FromHal(Explore.Blazor.Client.Clients.Providers provider) => new()
    {
        Provider = StorageProviderOptions.Normalize(provider.Provider),
        UsedBytes = provider.UsedBytes ?? 0,
        ReservedBytes = provider.ReservedBytes ?? 0,
        QuarantinedBytes = provider.QuarantinedBytes ?? 0,
        ObjectCount = provider.ObjectCount ?? 0,
        LastRecalculatedAt = provider.LastRecalculatedAt
    };

    public static StorageProviderUsageModel FromDto(InstanceStorageProviderUsageDto provider) => new()
    {
        Provider = StorageProviderOptions.Normalize(provider.Provider),
        UsedBytes = provider.UsedBytes ?? 0,
        ReservedBytes = provider.ReservedBytes ?? 0,
        QuarantinedBytes = provider.QuarantinedBytes ?? 0,
        ObjectCount = provider.ObjectCount ?? 0,
        LastRecalculatedAt = provider.LastRecalculatedAt
    };
}

public sealed class StorageProviderStatusModel
{
    public string Provider { get; set; } = StorageProviderOptions.Local;
    public bool IsAvailable { get; set; }
    public bool SupportsServerSideStreaming { get; set; }
    public bool SupportsBrowserDirectUpload { get; set; }
    public string FailureCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public static StorageProviderStatusModel FromHal(ProviderStatus? status) => new()
    {
        Provider = StorageProviderOptions.Normalize(status?.Provider),
        IsAvailable = status?.IsAvailable ?? false,
        SupportsServerSideStreaming = status?.SupportsServerSideStreaming ?? false,
        SupportsBrowserDirectUpload = status?.SupportsBrowserDirectUpload ?? false,
        FailureCode = status?.FailureCode ?? string.Empty,
        Message = status?.Message ?? string.Empty
    };

    public static StorageProviderStatusModel FromDto(InstanceStorageProviderStatusDto? status) => new()
    {
        Provider = StorageProviderOptions.Normalize(status?.Provider),
        IsAvailable = status?.IsAvailable ?? false,
        SupportsServerSideStreaming = status?.SupportsServerSideStreaming ?? false,
        SupportsBrowserDirectUpload = status?.SupportsBrowserDirectUpload ?? false,
        FailureCode = status?.FailureCode ?? string.Empty,
        Message = status?.Message ?? string.Empty
    };
}

public sealed class StorageConnectionTestResult
{
    public bool Success { get; set; }
    public string Provider { get; set; } = StorageProviderOptions.Local;
    public string Message { get; set; } = string.Empty;

    public static StorageConnectionTestResult FromStatus(StorageProviderStatusModel status) => new()
    {
        Success = status.IsAvailable,
        Provider = status.Provider,
        Message = string.IsNullOrWhiteSpace(status.Message)
            ? status.IsAvailable ? "Storage provider is available." : "Storage provider is unavailable."
            : status.Message
    };
}

public sealed class StorageUsageOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public StorageUsageModel Usage { get; set; } = new();
}
