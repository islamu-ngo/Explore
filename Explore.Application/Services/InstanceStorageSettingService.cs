// ABOUTME: Service implementation for managing instance S3 storage configuration.
// ABOUTME: Handles S3-compatible object storage settings for the application.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;

namespace Explore.Application.Services;

public class InstanceStorageSettingService : IInstanceStorageSettingService
{
    private readonly ISystemSettingRepository _systemSettingRepository;

    public InstanceStorageSettingService(ISystemSettingRepository systemSettingRepository)
    {
        _systemSettingRepository = systemSettingRepository;
    }

    public async Task<InstanceStorageSettingsDto> ReadSettingsAsync()
    {
        var endpoint = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.S3Endpoint);
        var publicEndpoint = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.S3PublicEndpoint);
        var bucketName = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.S3BucketName);
        var accessKeyId = await _systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Storage.AccessKeyId);
        var secretAccessKey = await _systemSettingRepository.GetByKey(InfrastructureSecretSettingKeys.Storage.SecretAccessKey);
        var region = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.S3Region);
        var forcePathStyle = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.S3ForcePathStyle);
        var uploadExpiration = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.S3UploadUrlExpirationMinutes);

        return new InstanceStorageSettingsDto
        {
            S3Endpoint = DeserializeString(endpoint?.Value, string.Empty),
            S3PublicEndpoint = DeserializeString(publicEndpoint?.Value, string.Empty),
            S3BucketName = DeserializeString(bucketName?.Value, string.Empty),
            S3AccessKeyId = DeserializeString(accessKeyId?.Value, string.Empty),
            S3SecretAccessKey = DeserializeString(secretAccessKey?.Value, string.Empty),
            S3Region = DeserializeString(region?.Value, "fsn1"),
            S3ForcePathStyle = DeserializeBoolean(forcePathStyle?.Value, true),
            S3UploadUrlExpirationMinutes = DeserializeInt(uploadExpiration?.Value, 60)
        };
    }

    public async Task ApplySettingsAsync(InstanceStorageSettingsDto settings)
    {
        await UpsertSystemSettingAsync(GovernanceSettingKeys.S3Endpoint,
            JsonSerializer.Serialize(settings.S3Endpoint.Trim()), SettingValueType.String, false,
            "ObjectStorage", 1, "S3-compatible endpoint URL (e.g., https://fsn1.your-objectstorage.com)");

        await UpsertSystemSettingAsync(GovernanceSettingKeys.S3PublicEndpoint,
            JsonSerializer.Serialize(settings.S3PublicEndpoint.Trim()), SettingValueType.String, false,
            "ObjectStorage", 2, "Public endpoint for presigned URLs (if different from internal endpoint)");

        await UpsertSystemSettingAsync(GovernanceSettingKeys.S3BucketName,
            JsonSerializer.Serialize(settings.S3BucketName.Trim()), SettingValueType.String, false,
            "ObjectStorage", 3, "S3 bucket name for object storage");

        await UpsertSystemSettingAsync(InfrastructureSecretSettingKeys.Storage.AccessKeyId,
            JsonSerializer.Serialize(settings.S3AccessKeyId.Trim()), SettingValueType.String, false,
            "ObjectStorage", 4, "S3 access key ID for authentication");

        await UpsertSystemSettingAsync(InfrastructureSecretSettingKeys.Storage.SecretAccessKey,
            JsonSerializer.Serialize(settings.S3SecretAccessKey.Trim()), SettingValueType.String, false,
            "ObjectStorage", 5, "S3 secret access key for authentication");

        await UpsertSystemSettingAsync(GovernanceSettingKeys.S3Region,
            JsonSerializer.Serialize(settings.S3Region.Trim()), SettingValueType.String, false,
            "ObjectStorage", 6, "S3 region identifier (e.g., fsn1 for Hetzner, us-east-1 for AWS)");

        await UpsertSystemSettingAsync(GovernanceSettingKeys.S3ForcePathStyle,
            JsonSerializer.Serialize(settings.S3ForcePathStyle), SettingValueType.Boolean, false,
            "ObjectStorage", 7, "Use path-style URLs (required by most non-AWS S3 providers)");

        await UpsertSystemSettingAsync(GovernanceSettingKeys.S3UploadUrlExpirationMinutes,
            JsonSerializer.Serialize(settings.S3UploadUrlExpirationMinutes > 0 ? settings.S3UploadUrlExpirationMinutes : 60),
            SettingValueType.Integer, false,
            "ObjectStorage", 8, "Presigned upload URL expiration time in minutes");
    }

    private static int DeserializeInt(string? rawValue, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<int>(rawValue);
        }
        catch
        {
            return int.TryParse(rawValue, out var parsed) ? parsed : defaultValue;
        }
    }

    private static bool DeserializeBoolean(string? rawValue, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<bool>(rawValue);
        }
        catch
        {
            return bool.TryParse(rawValue, out var parsed) ? parsed : defaultValue;
        }
    }

    private static string DeserializeString(string? rawValue, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue);
            return string.IsNullOrWhiteSpace(deserialized) ? defaultValue : deserialized;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }

    private async Task UpsertSystemSettingAsync(
        string settingKey,
        string value,
        SettingValueType valueType,
        bool isLocked,
        string category,
        int displayOrder,
        string description)
    {
        var existing = await _systemSettingRepository.GetByKey(settingKey);

        if (existing == null)
        {
            await _systemSettingRepository.Create(new SystemSetting
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

        await _systemSettingRepository.Update(existing);
    }
}
