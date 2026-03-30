// ABOUTME: FluentValidation validator for UpdateTenantNavigationLinkDto.
// ABOUTME: Same URL safety rules as Create, plus requires a valid Id.
using FluentValidation;

namespace Explore.Application.DTOs.Tenant.Validators;

public class UpdateTenantNavigationLinkDtoValidator : AbstractValidator<UpdateTenantNavigationLinkDto>
{
    /// <summary>
    /// Dangerous URL schemes that must be rejected to prevent XSS and injection attacks.
    /// </summary>
    private static readonly string[] ForbiddenSchemes =
        ["javascript:", "data:", "file:", "vbscript:", "mailto:"];

    public UpdateTenantNavigationLinkDtoValidator()
    {
        RuleFor(p => p.Id)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(p => p.Label)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.")
            .Must(label => !ContainsControlChars(label)).WithMessage("{PropertyName} contains invalid characters.");

        RuleFor(p => p.Url)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.")
            .Must(url => !ContainsControlChars(url)).WithMessage("{PropertyName} contains invalid characters.")
            .Must(BeASafeUrl).WithMessage("{PropertyName} must be a relative path starting with '/' or an http/https URL.");

        RuleFor(p => p.Icon)
            .MaximumLength(100).WithMessage("{PropertyName} must not exceed 100 characters.")
            .Must(icon => icon == null || !ContainsControlChars(icon)).WithMessage("{PropertyName} contains invalid characters.");
    }

    private static bool BeASafeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();

        // Reject protocol-relative URLs (//example.com)
        if (trimmed.StartsWith("//"))
            return false;

        // Reject forbidden schemes
        foreach (var scheme in ForbiddenSchemes)
        {
            if (trimmed.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Allow single "/" relative paths
        if (trimmed.StartsWith('/'))
            return true;

        // Allow http:// and https://
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool ContainsControlChars(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var c in value)
        {
            if (char.IsControl(c) && c != '\t')
                return true;
        }

        return false;
    }
}
