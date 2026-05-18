// ABOUTME: Validates full-document tenant branding settings replacement requests.
// ABOUTME: Keeps typed settings writes bounded to safe non-secret branding payload fields.

namespace Explore.Application.DTOs.TenantSettingsDocuments.Validators;

using FluentValidation;

public sealed class ReplaceTenantBrandingSettingsDocumentDtoValidator : AbstractValidator<ReplaceTenantBrandingSettingsDocumentDto>
{
    public ReplaceTenantBrandingSettingsDocumentDtoValidator()
    {
        RuleFor(request => request.ExpectedConcurrencyStamp)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(request => request.Payload)
            .NotNull().WithMessage("{PropertyName} is required.");

        When(request => request.Payload is not null, () =>
        {
            RuleFor(request => request.Payload.DisplayName)
                .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");

            RuleFor(request => request.Payload.LogoUrl)
                .MaximumLength(2048).WithMessage("{PropertyName} must not exceed 2048 characters.");

            RuleFor(request => request.Payload.FaviconUrl)
                .MaximumLength(2048).WithMessage("{PropertyName} must not exceed 2048 characters.");

            RuleFor(request => request.Payload.CustomCssUrl)
                .MaximumLength(2048).WithMessage("{PropertyName} must not exceed 2048 characters.");
        });
    }
}
