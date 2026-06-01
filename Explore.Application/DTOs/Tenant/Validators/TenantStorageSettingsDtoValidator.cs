// ABOUTME: FluentValidation rules for tenant-level storage admin settings.
// ABOUTME: Enforces provider allow-listing, byte ceilings, quotas, and optional S3 URL shape.

using Explore.Domain;
using FluentValidation;

namespace Explore.Application.DTOs.Tenant.Validators;

public sealed class TenantStorageSettingsDtoValidator : AbstractValidator<TenantStorageSettingsDto>
{
    private static readonly string[] AllowedProviders =
    [
        StorageProviders.Local,
        StorageProviders.S3Compatible
    ];

    public TenantStorageSettingsDtoValidator(long instanceMaxUploadBytes)
    {
        var ceilingBytes = instanceMaxUploadBytes > 0 ? instanceMaxUploadBytes : 1;

        RuleFor(settings => settings.Provider)
            .Must(provider => AllowedProviders.Contains(NormalizeProvider(provider)))
            .WithMessage("Storage provider must be local or s3_compatible.");

        RuleFor(settings => settings.MaxUploadBytes)
            .GreaterThan(0)
            .WithMessage("Max upload bytes must be greater than zero.");

        RuleFor(settings => settings.MaxUploadBytes)
            .LessThanOrEqualTo(ceilingBytes)
            .WithMessage($"Max upload bytes cannot exceed the instance ceiling of {ceilingBytes} bytes.");

        RuleFor(settings => settings.TenantQuotaBytes)
            .GreaterThan(0)
            .WithMessage("Tenant quota bytes must be greater than zero.");

        RuleFor(settings => settings.S3Endpoint)
            .Must(BeEmptyOrAbsoluteHttpUri)
            .WithMessage("S3 endpoint must be an absolute HTTP or HTTPS URI.");

        RuleFor(settings => settings.S3PublicEndpoint)
            .Must(BeEmptyOrAbsoluteHttpUri)
            .WithMessage("S3 public endpoint must be an absolute HTTP or HTTPS URI.");

        RuleFor(settings => settings.S3UploadUrlExpirationMinutes)
            .InclusiveBetween(1, 1440)
            .WithMessage("S3 upload URL expiration must be between 1 and 1440 minutes.");
    }

    private static string NormalizeProvider(string? provider)
        => provider?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool BeEmptyOrAbsoluteHttpUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
