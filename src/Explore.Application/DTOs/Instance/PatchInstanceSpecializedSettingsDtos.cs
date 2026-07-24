// ABOUTME: Presence-aware write contracts for specialized instance settings resources.
// ABOUTME: Keeps credentials and provider transitions grouped while preserving omitted configuration.

using Explore.Application.DTOs.Analytics;
using Explore.Application.DTOs.Storage;
using Explore.Application.Models.Common;
using Explore.Domain.Enums.Analytics;

namespace Explore.Application.DTOs.Instance;

public sealed class PatchInstanceStorageSettingsDto
{
    public OptionalUpdate<InstanceStoragePolicyWriteDto> Policy { get; set; } = OptionalUpdate<InstanceStoragePolicyWriteDto>.Unspecified();
    public OptionalUpdate<InstanceS3ConfigurationWriteDto> S3Configuration { get; set; } = OptionalUpdate<InstanceS3ConfigurationWriteDto>.Unspecified();
    public bool HasChanges() => Policy.HasValue || S3Configuration.HasValue;
}

public sealed class InstanceStoragePolicyWriteDto
{
    public string Provider { get; set; } = string.Empty;
    public long DefaultMaxUploadBytes { get; set; }
    public long DefaultTenantQuotaBytes { get; set; }
    public long InstanceMaxUploadBytes { get; set; }
    public bool LockTenantStorage { get; set; }
    public List<StorageRouteSettingsDto> Routes { get; set; } = [];
}

public sealed class InstanceS3ConfigurationWriteDto
{
    public string Endpoint { get; set; } = string.Empty;
    public string PublicEndpoint { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; }
    public int UploadUrlExpirationMinutes { get; set; }
}

public sealed class PatchInstanceSmtpSettingsDto
{
    public OptionalUpdate<InstanceSmtpConfigurationWriteDto> Configuration { get; set; } = OptionalUpdate<InstanceSmtpConfigurationWriteDto>.Unspecified();
    public bool HasChanges() => Configuration.HasValue;
}

public sealed class InstanceSmtpConfigurationWriteDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Security { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
    public bool SkipCertificateValidation { get; set; }
}

public sealed class PatchResolverConfigurationDto
{
    public OptionalUpdate<bool> HeaderEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> SubdomainEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> CustomDomainEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> PathEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<string?> PathPrefix { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> InstanceBaseDomain { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> AllowTenantCustomDomains { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => HeaderEnabled.HasValue || SubdomainEnabled.HasValue || CustomDomainEnabled.HasValue
        || PathEnabled.HasValue || PathPrefix.HasValue || InstanceBaseDomain.HasValue || AllowTenantCustomDomains.HasValue;
}

public sealed class PatchAnalyticsGovernanceSettingsDto
{
    public OptionalUpdate<bool> CookieConsentEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<DeclineBehavior> DeclineBehavior { get; set; } = OptionalUpdate<DeclineBehavior>.Unspecified();
    public OptionalUpdate<int> ConsentCookieLifetimeDays { get; set; } = OptionalUpdate<int>.Unspecified();
    public OptionalUpdate<bool> GlobalDisableClientTracking { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<PosthogCookielessMode> PosthogCookielessMode { get; set; } = OptionalUpdate<PosthogCookielessMode>.Unspecified();
    public OptionalUpdate<PosthogPersonProfiles> PosthogPersonProfiles { get; set; } = OptionalUpdate<PosthogPersonProfiles>.Unspecified();
    public OptionalUpdate<bool> PosthogSessionReplay { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> PosthogAutocapture { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> PosthogHeatmaps { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> PosthogToolbar { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => CookieConsentEnabled.HasValue || DeclineBehavior.HasValue || ConsentCookieLifetimeDays.HasValue
        || GlobalDisableClientTracking.HasValue || PosthogCookielessMode.HasValue || PosthogPersonProfiles.HasValue
        || PosthogSessionReplay.HasValue || PosthogAutocapture.HasValue || PosthogHeatmaps.HasValue || PosthogToolbar.HasValue;
}

public sealed class PatchAuthProviderConfigurationDto
{
    public OptionalUpdate<AuthProviderConfigurationWriteDto> Configuration { get; set; } = OptionalUpdate<AuthProviderConfigurationWriteDto>.Unspecified();
    public bool HasChanges() => Configuration.HasValue;
}

public sealed class AuthProviderConfigurationWriteDto
{
    public bool KeycloakEnabled { get; set; }
    public string KeycloakAuthority { get; set; } = string.Empty;
    public string KeycloakClientId { get; set; } = string.Empty;
    public string KeycloakClientSecret { get; set; } = string.Empty;
    public bool AtprotoLoginEnabled { get; set; }
    public string AtprotoPublicUrl { get; set; } = string.Empty;
    public bool GoogleSsoEnabled { get; set; }
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;
    public bool LockKeycloakEnabled { get; set; }
    public bool LockAtprotoLoginEnabled { get; set; }
    public bool LockGoogleSsoEnabled { get; set; }
}

public sealed class PatchAuthorizationProviderConfigurationDto
{
    public OptionalUpdate<AuthorizationProviderConfigurationWriteDto> Configuration { get; set; } = OptionalUpdate<AuthorizationProviderConfigurationWriteDto>.Unspecified();
    public bool HasChanges() => Configuration.HasValue;
}

public sealed class AuthorizationProviderConfigurationWriteDto
{
    public string Provider { get; set; } = string.Empty;
    public string CerbosGrpcEndpoint { get; set; } = string.Empty;
    public string CerbosAdminEndpoint { get; set; } = string.Empty;
    public string? CerbosAdminUsername { get; set; }
    public string? CerbosAdminPassword { get; set; }
}
