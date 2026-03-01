// ABOUTME: Setting definitions for S3-compatible object storage configuration.
// ABOUTME: Sensitive keys (access key, secret key) are flagged with IsSensitive = true.

namespace Explore.Domain.Settings.Definitions;

public static class StorageSettingDefinitions
{
    public static readonly SettingDefinition Endpoint = new(
        Key: "s3.endpoint",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "ObjectStorage",
        Description: "S3-compatible endpoint URL (e.g., https://fsn1.your-objectstorage.com)",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition PublicEndpoint = new(
        Key: "s3.public_endpoint",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "ObjectStorage",
        Description: "Public endpoint for presigned URLs (if different from internal endpoint)",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition BucketName = new(
        Key: "s3.bucket_name",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "ObjectStorage",
        Description: "S3 bucket name for object storage",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition AccessKeyId = new(
        Key: "s3.access_key_id",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "ObjectStorage",
        Description: "S3 access key ID for authentication",
        MaxScope: SettingScope.Tenant,
        IsSensitive: true);

    public static readonly SettingDefinition SecretAccessKey = new(
        Key: "s3.secret_access_key",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "ObjectStorage",
        Description: "S3 secret access key for authentication (stored encrypted)",
        MaxScope: SettingScope.Tenant,
        IsSensitive: true);

    public static readonly SettingDefinition Region = new(
        Key: "s3.region",
        ValueType: SettingValueType.String,
        DefaultValue: "\"fsn1\"",
        Category: "ObjectStorage",
        Description: "S3 region identifier (e.g., fsn1 for Hetzner, us-east-1 for AWS)",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition ForcePathStyle = new(
        Key: "s3.force_path_style",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "ObjectStorage",
        Description: "Use path-style URLs (required by most non-AWS S3 providers)",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition UploadUrlExpirationMinutes = new(
        Key: "s3.upload_url_expiration_minutes",
        ValueType: SettingValueType.Integer,
        DefaultValue: "60",
        Category: "ObjectStorage",
        Description: "Presigned upload URL expiration time in minutes",
        MaxScope: SettingScope.Tenant);

    public static IReadOnlyList<SettingDefinition> All =>
    [
        Endpoint, PublicEndpoint, BucketName, AccessKeyId, SecretAccessKey,
        Region, ForcePathStyle, UploadUrlExpirationMinutes
    ];
}
