// ABOUTME: Strongly-typed S3/Object Storage setting group resolved via batch loading.
// ABOUTME: Replaces the N+1 pattern in S3ConfigResolver with a single ResolveGroupAsync call.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Strongly-typed group for all S3/object-storage settings.
/// </summary>
public class StorageSettingGroup : ISettingGroup
{
    public string Provider { get; private set; } = "minio";
    public string? Endpoint { get; private set; }
    public string? BucketName { get; private set; }
    public string? Region { get; private set; }
    public string? AccessKey { get; private set; }
    public string? SecretKey { get; private set; }
    public bool UseHttps { get; private set; } = true;
    public int MaxFileSizeMb { get; private set; } = 10;

    public static IEnumerable<string> SettingKeys =>
    [
        "storage.provider", "storage.endpoint", "storage.bucket_name",
        "storage.region", "storage.access_key", "storage.secret_key",
        "storage.use_https", "storage.max_file_size_mb"
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue("storage.provider", out var provider))
            Provider = SettingValueSerializer.Deserialize(provider.Value, "minio");
        if (settings.TryGetValue("storage.endpoint", out var ep))
            Endpoint = SettingValueSerializer.DeserializeString(ep.Value);
        if (settings.TryGetValue("storage.bucket_name", out var bucket))
            BucketName = SettingValueSerializer.DeserializeString(bucket.Value);
        if (settings.TryGetValue("storage.region", out var region))
            Region = SettingValueSerializer.DeserializeString(region.Value);
        if (settings.TryGetValue("storage.access_key", out var ak))
            AccessKey = SettingValueSerializer.DeserializeString(ak.Value);
        if (settings.TryGetValue("storage.secret_key", out var sk))
            SecretKey = SettingValueSerializer.DeserializeString(sk.Value);
        if (settings.TryGetValue("storage.use_https", out var https))
            UseHttps = SettingValueSerializer.Deserialize(https.Value, true);
        if (settings.TryGetValue("storage.max_file_size_mb", out var maxSize))
            MaxFileSizeMb = SettingValueSerializer.Deserialize(maxSize.Value, 10);
    }
}
