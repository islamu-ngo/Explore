using FluentValidation;

namespace Explore.Application.DTOs.Tag.Validators
{
    public class UpdateTagDtoValidator : AbstractValidator<UpdateTagDto>
    {
        public UpdateTagDtoValidator()
        {
            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.");

            RuleFor(p => p.MasterCode)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

            RuleFor(p => p.FullName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .MaximumLength(500).WithMessage("{PropertyName} must not exceed 500 characters.");

            RuleFor(p => p.TenantId)
                .NotEmpty().WithMessage("{PropertyName} is required.");
        }
    }
}
