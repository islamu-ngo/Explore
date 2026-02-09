using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.Category.Validators;

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;

    public UpdateCategoryDtoValidator(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;

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

        RuleFor(p => p.ParentId)
            .MustAsync(async (id, cancellation) =>
            {
                if (!id.HasValue) return true;
                var exists = await _categoryRepository.Exists(id.Value);
                return exists;
            }).WithMessage("{PropertyName} does not exist.")
            .Must((dto, parentId) => parentId != dto.Id)
            .WithMessage("A category cannot be its own parent.");
    }
}
