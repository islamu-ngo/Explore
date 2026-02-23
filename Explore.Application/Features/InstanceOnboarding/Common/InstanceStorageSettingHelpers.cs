// ABOUTME: Shared helpers for reading and writing instance S3 storage settings in SystemSetting records.
// ABOUTME: Centralizes serialization, parsing, and upsert logic for the storage settings CQRS pipeline.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;

namespace Explore.Application.Features.InstanceOnboarding.Common;

internal static class InstanceStorageSettingHelpers
{
    internal static async Task<InstanceStorageSettingsDto> ReadSettingsAsync(
        ISystemSettingRepository systemSettingRepository)
    {
        var endpoint = await systemSettingRepository.GetByKey(GovernanceSettingKeys.S3Endpoint);
        var publicEndpoint = await systemSettingRepository.GetByKey(GovernanceSettingKeys.S3PublicEndpoint);
        var bucketName = await systemSettingRepository.GetByKey(GovernanceSettingKeys.S3BucketName);
        var accessKeyId = await systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Storage.AccessKeyId);
        var secretAccessKey = await systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Storage.SecretAccessKey);
        var region = await systemSettingRepository.GetByKey(GovernanceSettingKeys.S3Region);
        var forcePathStyle = await systemSettingRepository.GetByKey(GovernanceSettingKeys.S3ForcePathStyle);
        var uploadExpiration = await systemSettingRepository.GetByKey(GovernanceSettingKeys.S3UploadUrlExpirationMinutes);

        return new InstanceStorageSettingsDto
        {
            S3Endpoint = InstanceGovernanceSettingHelpers.DeserializeString(endpoint?.Value, string.Empty),
            S3PublicEndpoint = InstanceGovernanceSettingHelpers.DeserializeString(publicEndpoint?.Value, string.Empty),
            S3BucketName = InstanceGovernanceSettingHelpers.DeserializeString(bucketName?.Value, string.Empty),
            S3AccessKeyId = InstanceGovernanceSettingHelpers.DeserializeString(accessKeyId?.Value, string.Empty),
            S3SecretAccessKey = InstanceGovernanceSettingHelpers.DeserializeString(secretAccessKey?.Value, string.Empty),
            S3Region = InstanceGovernanceSettingHelpers.DeserializeString(region?.Value, "fsn1"),
            S3ForcePathStyle = InstanceGovernanceSettingHelpers.DeserializeBoolean(forcePathStyle?.Value, true),
            S3UploadUrlExpirationMinutes = InstanceGovernanceSettingHelpers.DeserializeInt(uploadExpiration?.Value, 60)
        };
    }

    internal static async Task ApplySettingsAsync(
        ISystemSettingRepository systemSettingRepository,
        InstanceStorageSettingsDto settings)
    {
        await UpsertSystemSettingAsync(systemSettingRepository, GovernanceSettingKeys.S3Endpoint,
            JsonSerializer.Serialize(settings.S3Endpoint.Trim()), SettingValueType.String, false,
            "ObjectStorage", 1, "S3-compatible endpoint URL (e.g., https://fsn1.your-objectstorage.com)");

        await UpsertSystemSettingAsync(systemSettingRepository, GovernanceSettingKeys.S3PublicEndpoint,
            JsonSerializer.Serialize(settings.S3PublicEndpoint.Trim()), SettingValueType.String, false,
            "ObjectStorage", 2, "Public endpoint for presigned URLs (if different from internal endpoint)");

        await UpsertSystemSettingAsync(systemSettingRepository, GovernanceSettingKeys.S3BucketName,
            JsonSerializer.Serialize(settings.S3BucketName.Trim()), SettingValueType.String, false,
            "ObjectStorage", 3, "S3 bucket name for object storage");

        await UpsertSystemSettingAsync(systemSettingRepository, InfrastructureSecretSettingKeys.Storage.AccessKeyId,
            JsonSerializer.Serialize(settings.S3AccessKeyId.Trim()), SettingValueType.String, false,
            "ObjectStorage", 4, "S3 access key ID for authentication");

        await UpsertSystemSettingAsync(systemSettingRepository, InfrastructureSecretSettingKeys.Storage.SecretAccessKey,
            JsonSerializer.Serialize(settings.S3SecretAccessKey.Trim()), SettingValueType.String, false,
            "ObjectStorage", 5, "S3 secret access key for authentication");

        await UpsertSystemSettingAsync(systemSettingRepository, GovernanceSettingKeys.S3Region,
            JsonSerializer.Serialize(settings.S3Region.Trim()), SettingValueType.String, false,
            "ObjectStorage", 6, "S3 region identifier (e.g., fsn1 for Hetzner, us-east-1 for AWS)");

        await UpsertSystemSettingAsync(systemSettingRepository, GovernanceSettingKeys.S3ForcePathStyle,
            JsonSerializer.Serialize(settings.S3ForcePathStyle), SettingValueType.Boolean, false,
            "ObjectStorage", 7, "Use path-style URLs (required by most non-AWS S3 providers)");

        await UpsertSystemSettingAsync(systemSettingRepository, GovernanceSettingKeys.S3UploadUrlExpirationMinutes,
            JsonSerializer.Serialize(settings.S3UploadUrlExpirationMinutes > 0 ? settings.S3UploadUrlExpirationMinutes : 60),
            SettingValueType.Integer, false,
            "ObjectStorage", 8, "Presigned upload URL expiration time in minutes");
    }

    private static async Task UpsertSystemSettingAsync(
        ISystemSettingRepository systemSettingRepository,
        string settingKey,
        string value,
        SettingValueType valueType,
        bool isLocked,
        string category,
        int displayOrder,
        string description)
    {
        var existing = await systemSettingRepository.GetByKey(settingKey);

        if (existing == null)
        {
            await systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = settingKey,
                Value = value,
                ValueType = valueType,
                IsLocked = isLocked,
                Description = description,
                Category = category,
                DisplayOrder = displayOrder,
                CreatedAt = DateTime.UtcNow
            });

            return;
        }

        existing.Value = value;
        existing.ValueType = valueType;
        existing.IsLocked = isLocked;
        existing.Description = description;
        existing.Category = category;
        existing.DisplayOrder = displayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await systemSettingRepository.Update(existing);
    }
}
