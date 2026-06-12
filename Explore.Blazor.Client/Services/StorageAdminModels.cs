// ABOUTME: UI-facing models for provider-neutral instance and tenant storage administration.
// ABOUTME: Maps regenerated HAL storage DTOs into editable Blazor state and action affordances.

using System.Text.Json;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public static class StorageRouteOptions
{
    public const string Images = "images";
    public const string Documents = "documents";
    public const string General = "general";

    public static readonly string[] All = [Images, Documents, General];

    public static string Normalize(string? routeKey) =>
        All.Contains(routeKey?.Trim().ToLowerInvariant() ?? string.Empty, StringComparer.Ordinal)
            ? routeKey!.Trim().ToLowerInvariant()
            : General;

    public static string Label(string? routeKey) => Normalize(routeKey) switch
    {
        Images => "Images",
        Documents => "Documents",
        _ => "General uploads"
    };

    public static string Description(string? routeKey) => Normalize(routeKey) switch
    {
        Images => "Profile, event, and image MIME uploads.",
        Documents => "PDF, office, and document MIME uploads.",
        _ => "Attachments and uploads that do not match a specialized route."
    };
}

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
    public List<StorageRouteSettingsModel> Routes { get; set; } = StorageRouteSettingsModel.Defaults(10 * MiB);
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
        Routes = StorageRouteSettingsModel.FromGeneratedRoutes(resource.Routes?.Cast<object>(), resource.AdditionalProperties, PositiveOrDefault(resource.DefaultMaxUploadBytes, 10 * MiB)),
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

    public InstanceStorageSettingsDto ToDto() => new InstanceStorageSettingsDto()
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
    }.WithRoutes(Routes);

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
    public List<StorageRouteSettingsModel> Routes { get; set; } = StorageRouteSettingsModel.Defaults(10 * MiB, isReadOnly: true);
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
        Routes = StorageRouteSettingsModel.FromGeneratedRoutes(resource.Routes?.Cast<object>(), resource.AdditionalProperties, PositiveOrDefault(resource.MaxUploadBytes, 10 * MiB), resource.IsReadOnly ?? true),
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

    public TenantStorageSettingsDto ToDto() => new TenantStorageSettingsDto()
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
    }.WithRoutes(Routes);

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


public static class StorageRouteDtoExtensions
{
    public static InstanceStorageSettingsDto WithRoutes(
        this InstanceStorageSettingsDto dto,
        IEnumerable<StorageRouteSettingsModel> routes)
    {
        dto.Routes = StorageRouteSettingsModel.ToDtos(routes);
        return dto;
    }

    public static TenantStorageSettingsDto WithRoutes(
        this TenantStorageSettingsDto dto,
        IEnumerable<StorageRouteSettingsModel> routes)
    {
        dto.Routes = StorageRouteSettingsModel.ToDtos(routes);
        return dto;
    }
}

public sealed class StorageRouteSettingsModel
{
    private const long MiB = 1024L * 1024L;

    public string RouteKey { get; set; } = StorageRouteOptions.General;
    public string Provider { get; set; } = StorageProviderOptions.Local;
    public long MaxUploadBytes { get; set; } = 10 * MiB;
    public string ProviderSource { get; set; } = "SystemDefault";
    public string MaxUploadSource { get; set; } = "SystemDefault";
    public bool IsReadOnly { get; set; }

    public string RouteLabel => StorageRouteOptions.Label(RouteKey);
    public string RouteDescription => StorageRouteOptions.Description(RouteKey);
    public string ProviderLabel => StorageProviderOptions.Label(Provider);

    public long MaxUploadMiB
    {
        get => Math.Max(1, MaxUploadBytes / MiB);
        set => MaxUploadBytes = Math.Max(1, value) * MiB;
    }

    public static List<StorageRouteSettingsModel> Defaults(long fallbackMaxUploadBytes, bool isReadOnly = false) =>
        StorageRouteOptions.All.Select(routeKey => new StorageRouteSettingsModel
        {
            RouteKey = routeKey,
            Provider = StorageProviderOptions.Local,
            MaxUploadBytes = PositiveOrDefault(fallbackMaxUploadBytes),
            IsReadOnly = isReadOnly
        }).ToList();

    public static List<StorageRouteSettingsModel> FromExtensionData(
        IDictionary<string, object>? additionalProperties,
        long fallbackMaxUploadBytes,
        bool isReadOnly = false)
    {
        var routes = Defaults(fallbackMaxUploadBytes, isReadOnly);
        if (additionalProperties is null || !additionalProperties.TryGetValue("routes", out var value))
        {
            return routes;
        }

        foreach (var route in ReadRoutes(value, fallbackMaxUploadBytes, isReadOnly))
        {
            var index = routes.FindIndex(existing => existing.RouteKey == route.RouteKey);
            if (index >= 0)
            {
                routes[index] = route;
            }
        }

        return routes;
    }

    public static List<StorageRouteSettingsModel> FromDtos(
        IEnumerable<StorageRouteSettingsDto>? routeDtos,
        IDictionary<string, object>? additionalProperties,
        long fallbackMaxUploadBytes,
        bool isReadOnly = false)
    {
        if (routeDtos is null)
        {
            return FromExtensionData(additionalProperties, fallbackMaxUploadBytes, isReadOnly);
        }

        var routes = Defaults(fallbackMaxUploadBytes, isReadOnly);
        foreach (var route in routeDtos.Select(route => FromDto(route, fallbackMaxUploadBytes, isReadOnly)))
        {
            var index = routes.FindIndex(existing => existing.RouteKey == route.RouteKey);
            if (index >= 0)
            {
                routes[index] = route;
            }
        }

        return routes;
    }

    public static List<StorageRouteSettingsModel> FromGeneratedRoutes(
        IEnumerable<object>? routeDtos,
        IDictionary<string, object>? additionalProperties,
        long fallbackMaxUploadBytes,
        bool isReadOnly = false)
    {
        if (routeDtos is null)
        {
            return FromExtensionData(additionalProperties, fallbackMaxUploadBytes, isReadOnly);
        }

        var routes = Defaults(fallbackMaxUploadBytes, isReadOnly);
        foreach (var route in routeDtos.Select(route => FromGeneratedRoute(route, fallbackMaxUploadBytes, isReadOnly)))
        {
            var index = routes.FindIndex(existing => existing.RouteKey == route.RouteKey);
            if (index >= 0)
            {
                routes[index] = route;
            }
        }

        return routes;
    }

    public static List<StorageRouteSettingsDto> ToDtos(IEnumerable<StorageRouteSettingsModel> routes) =>
        routes
            .GroupBy(route => StorageRouteOptions.Normalize(route.RouteKey), StringComparer.Ordinal)
            .Select(group => group.First())
            .Where(route => StorageRouteOptions.All.Contains(StorageRouteOptions.Normalize(route.RouteKey), StringComparer.Ordinal))
            .Select(route => new StorageRouteSettingsDto
            {
                RouteKey = StorageRouteOptions.Normalize(route.RouteKey),
                Provider = StorageProviderOptions.Normalize(route.Provider),
                MaxUploadBytes = PositiveOrDefault(route.MaxUploadBytes),
                ProviderSource = route.ProviderSource,
                MaxUploadSource = route.MaxUploadSource,
                IsReadOnly = route.IsReadOnly
            })
            .ToList();

    public static List<Dictionary<string, object?>> ToExtensionValue(IEnumerable<StorageRouteSettingsModel> routes) =>
        routes
            .GroupBy(route => StorageRouteOptions.Normalize(route.RouteKey), StringComparer.Ordinal)
            .Select(group => group.First())
            .Where(route => StorageRouteOptions.All.Contains(StorageRouteOptions.Normalize(route.RouteKey), StringComparer.Ordinal))
            .Select(route => new Dictionary<string, object?>
            {
                ["routeKey"] = StorageRouteOptions.Normalize(route.RouteKey),
                ["provider"] = StorageProviderOptions.Normalize(route.Provider),
                ["maxUploadBytes"] = PositiveOrDefault(route.MaxUploadBytes),
                ["providerSource"] = route.ProviderSource,
                ["maxUploadSource"] = route.MaxUploadSource,
                ["isReadOnly"] = route.IsReadOnly
            })
            .ToList();

    private static IEnumerable<StorageRouteSettingsModel> ReadRoutes(object value, long fallbackMaxUploadBytes, bool isReadOnly)
    {
        if (value is JsonElement { ValueKind: JsonValueKind.Array } array)
        {
            foreach (var item in array.EnumerateArray())
            {
                var route = FromJsonElement(item, fallbackMaxUploadBytes, isReadOnly);
                if (route is not null)
                {
                    yield return route;
                }
            }
        }
    }

    private static StorageRouteSettingsModel? FromJsonElement(JsonElement item, long fallbackMaxUploadBytes, bool isReadOnly)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var routeKey = ReadString(item, "routeKey");
        if (!StorageRouteOptions.All.Contains(StorageRouteOptions.Normalize(routeKey), StringComparer.Ordinal))
        {
            return null;
        }

        return new StorageRouteSettingsModel
        {
            RouteKey = StorageRouteOptions.Normalize(routeKey),
            Provider = StorageProviderOptions.Normalize(ReadString(item, "provider")),
            MaxUploadBytes = PositiveOrDefault(ReadLong(item, "maxUploadBytes") ?? fallbackMaxUploadBytes),
            ProviderSource = ReadString(item, "providerSource") ?? "SystemDefault",
            MaxUploadSource = ReadString(item, "maxUploadSource") ?? "SystemDefault",
            IsReadOnly = ReadBoolean(item, "isReadOnly") ?? isReadOnly
        };
    }

    private static StorageRouteSettingsModel FromDto(
        StorageRouteSettingsDto route,
        long fallbackMaxUploadBytes,
        bool isReadOnly) => new()
    {
        RouteKey = StorageRouteOptions.Normalize(route.RouteKey),
        Provider = StorageProviderOptions.Normalize(route.Provider),
        MaxUploadBytes = PositiveOrDefault(route.MaxUploadBytes ?? fallbackMaxUploadBytes),
        ProviderSource = route.ProviderSource ?? "SystemDefault",
        MaxUploadSource = route.MaxUploadSource ?? "SystemDefault",
        IsReadOnly = route.IsReadOnly ?? isReadOnly
    };

    private static StorageRouteSettingsModel FromGeneratedRoute(
        object route,
        long fallbackMaxUploadBytes,
        bool isReadOnly) => new()
    {
        RouteKey = StorageRouteOptions.Normalize(ReadProperty<string>(route, "RouteKey")),
        Provider = StorageProviderOptions.Normalize(ReadProperty<string>(route, "Provider")),
        MaxUploadBytes = PositiveOrDefault(ReadProperty<long?>(route, "MaxUploadBytes") ?? fallbackMaxUploadBytes),
        ProviderSource = ReadProperty<string>(route, "ProviderSource") ?? "SystemDefault",
        MaxUploadSource = ReadProperty<string>(route, "MaxUploadSource") ?? "SystemDefault",
        IsReadOnly = ReadProperty<bool?>(route, "IsReadOnly") ?? isReadOnly
    };

    private static string? ReadString(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static long? ReadLong(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value)
            ? value
            : null;

    private static bool? ReadBoolean(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out var property) &&
        (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
            ? property.GetBoolean()
            : null;

    private static T? ReadProperty<T>(object source, string propertyName) =>
        source.GetType().GetProperty(propertyName)?.GetValue(source) is T value ? value : default;

    private static long PositiveOrDefault(long value) => value > 0 ? value : 10 * MiB;
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
    public List<StorageRouteSettingsModel> Routes { get; set; } = [];

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
        QuotaSource = policy?.QuotaSource ?? "SystemDefault",
        Routes = StorageRouteSettingsModel.FromGeneratedRoutes(policy?.Routes?.Cast<object>(), policy?.AdditionalProperties, policy?.MaxUploadBytes ?? 0, isReadOnly: true)
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
        QuotaSource = policy?.QuotaSource ?? "SystemDefault",
        Routes = StorageRouteSettingsModel.FromGeneratedRoutes(policy?.Routes?.Cast<object>(), policy?.AdditionalProperties, policy?.MaxUploadBytes ?? 0, isReadOnly: true)
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
