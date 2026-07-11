using FluentValidation;

namespace Explore.Application.DTOs.Group.Validators;

public class CreateGroupDtoValidator : AbstractValidator<CreateGroupDto>
{
    public CreateGroupDtoValidator()
    {
        RuleFor(p => p.FullName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .NotNull()
            .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

        RuleFor(p => p.Description)
            .MaximumLength(5000)
            .When(p => !string.IsNullOrEmpty(p.Description))
            .WithMessage("{PropertyName} must not exceed 5000 characters.");

        RuleFor(p => p)
            .Must(p => !p.ParentOrganizationId.HasValue || !p.ParentGroupId.HasValue)
            .WithMessage("A group can have either a parent organization or a parent group, not both.");
    }
}
