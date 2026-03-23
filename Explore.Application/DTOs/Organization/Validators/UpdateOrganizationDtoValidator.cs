// ABOUTME: FluentValidation validator for UpdateOrganizationDto.
// ABOUTME: Manually instantiated in UpdateOrganizationDetailsCommandHandler (not DI-injected).

using FluentValidation;

namespace Explore.Application.DTOs.Organization.Validators;

public class UpdateOrganizationDtoValidator : AbstractValidator<UpdateOrganizationDto>
{
    public UpdateOrganizationDtoValidator()
    {
        RuleFor(p => p.FullName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(100).WithMessage("{PropertyName} must not exceed 100 characters.");

        RuleFor(p => p.WebsiteUrl)
            .MaximumLength(200).When(p => !string.IsNullOrEmpty(p.WebsiteUrl))
            .WithMessage("{PropertyName} must not exceed 200 characters.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            .When(p => !string.IsNullOrEmpty(p.WebsiteUrl))
            .WithMessage("{PropertyName} must be a valid URI.");

        RuleFor(p => p.Email)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .EmailAddress().WithMessage("{PropertyName} must be a valid email address.");

        RuleFor(p => p.Country)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.");

        RuleFor(p => p.City)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.");

        RuleFor(p => p.Address)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");
    }
}
