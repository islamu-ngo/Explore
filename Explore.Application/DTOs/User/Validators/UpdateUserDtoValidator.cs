// ABOUTME: FluentValidation validator for UpdateUserDto.
// ABOUTME: Manually instantiated in UpdateUserCommandHandler (not DI-injected).

using FluentValidation;

namespace Explore.Application.DTOs.User.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(p => p.Id)
            .NotEmpty().WithMessage("{PropertyName} is required.");

        RuleFor(p => p.FirstName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(100).WithMessage("{PropertyName} must not exceed 100 characters.");

        RuleFor(p => p.LastName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(100).WithMessage("{PropertyName} must not exceed 100 characters.");

        RuleFor(p => p.Email)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .EmailAddress().WithMessage("{PropertyName} must be a valid email address.");

        RuleFor(p => p.Username)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.");

        RuleFor(p => p.Bio)
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.")
            .When(p => !string.IsNullOrEmpty(p.Bio));

        RuleFor(p => p.City)
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.")
            .When(p => !string.IsNullOrEmpty(p.City));

        RuleFor(p => p.Country)
            .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters.")
            .When(p => !string.IsNullOrEmpty(p.Country));
    }
}
