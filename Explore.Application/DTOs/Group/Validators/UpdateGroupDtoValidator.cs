using FluentValidation;

namespace Explore.Application.DTOs.Group.Validators;

public class UpdateGroupDtoValidator : AbstractValidator<UpdateGroupDto>
{
    public UpdateGroupDtoValidator()
    {
        RuleFor(p => p.FullName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Description)
            .MaximumLength(5000)
            .When(p => !string.IsNullOrEmpty(p.Description))
            .WithMessage("{PropertyName} must not exceed 5000 characters.");
    }
}
