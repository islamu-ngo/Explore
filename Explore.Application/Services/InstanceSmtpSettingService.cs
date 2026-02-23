// ABOUTME: Service implementation for managing instance SMTP configuration.
// ABOUTME: Reads/writes SMTP settings from SystemSetting records using governance keys.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;

namespace Explore.Application.Services;

public class InstanceSmtpSettingService : IInstanceSmtpSettingService
{
    private readonly ISystemSettingRepository _systemSettingRepository;

    public InstanceSmtpSettingService(ISystemSettingRepository systemSettingRepository)
    {
        _systemSettingRepository = systemSettingRepository;
    }

    public async Task<InstanceSmtpSettingsDto> ReadSettingsAsync()
    {
        var host = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EmailSmtpHost);
        var port = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EmailSmtpPort);
        var username = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EmailSmtpUsername);
        var password = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EmailSmtpPassword);
        var security = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EmailSmtpSecurity);
        var fromAddress = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EmailFromAddress);
        var fromName = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EmailFromName);
        var timeout = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EmailSmtpTimeoutSeconds);
        var skipCertValidation = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EmailSmtpSkipCertValidation);

        return new InstanceSmtpSettingsDto
        {
            Host = DeserializeString(host?.Value, string.Empty),
            Port = DeserializeInt(port?.Value, 587),
            Username = DeserializeString(username?.Value, string.Empty),
            Password = DeserializeString(password?.Value, string.Empty),
            Security = DeserializeString(security?.Value, "StartTls"),
            FromAddress = DeserializeString(fromAddress?.Value, string.Empty),
            FromName = DeserializeString(fromName?.Value, string.Empty),
            TimeoutSeconds = DeserializeInt(timeout?.Value, 30),
            SkipCertificateValidation = DeserializeBoolean(skipCertValidation?.Value, false)
        };
    }

    public async Task ApplySettingsAsync(InstanceSmtpSettingsDto settings)
    {
        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EmailSmtpHost,
            JsonSerializer.Serialize(settings.Host.Trim()),
            SettingValueType.String,
            false,
            "Email",
            1,
            "SMTP host server name");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EmailSmtpPort,
            JsonSerializer.Serialize(settings.Port > 0 ? settings.Port : 587),
            SettingValueType.Integer,
            false,
            "Email",
            2,
            "SMTP server port");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EmailSmtpUsername,
            JsonSerializer.Serialize(settings.Username.Trim()),
            SettingValueType.String,
            false,
            "Email",
            3,
            "SMTP username");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EmailSmtpPassword,
            JsonSerializer.Serialize(settings.Password.Trim()),
            SettingValueType.String,
            false,
            "Email",
            4,
            "SMTP password or app token");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EmailSmtpSecurity,
            JsonSerializer.Serialize(settings.Security.Trim()),
            SettingValueType.String,
            false,
            "Email",
            5,
            "SMTP security mode: None, StartTls, SslOnConnect, or Auto");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EmailFromAddress,
            JsonSerializer.Serialize(settings.FromAddress.Trim()),
            SettingValueType.String,
            false,
            "Email",
            6,
            "Default sender email address");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EmailFromName,
            JsonSerializer.Serialize(settings.FromName.Trim()),
            SettingValueType.String,
            false,
            "Email",
            7,
            "Default sender display name");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EmailSmtpTimeoutSeconds,
            JsonSerializer.Serialize(settings.TimeoutSeconds > 0 ? settings.TimeoutSeconds : 30),
            SettingValueType.Integer,
            false,
            "Email",
            8,
            "SMTP connection timeout in seconds");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EmailSmtpSkipCertValidation,
            JsonSerializer.Serialize(settings.SkipCertificateValidation),
            SettingValueType.Boolean,
            false,
            "Email",
            9,
            "Skip TLS certificate validation (for development only)");
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
