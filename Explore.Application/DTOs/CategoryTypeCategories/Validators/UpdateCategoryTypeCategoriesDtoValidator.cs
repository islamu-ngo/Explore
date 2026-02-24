using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.CategoryTypeCategories.Validators;

public class UpdateCategoryTypeCategoriesDtoValidator : AbstractValidator<UpdateCategoryTypeCategoriesDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICategoryTypeRepository _categoryTypeRepository;

    public UpdateCategoryTypeCategoriesDtoValidator(
        ICategoryRepository categoryRepository,
        ICategoryTypeRepository categoryTypeRepository)
    {
        _categoryRepository = categoryRepository;
        _categoryTypeRepository = categoryTypeRepository;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required")
            .MustAsync(CategoryExists)
            .WithMessage("Category not found");

        RuleFor(x => x.CategoryTypeId)
            .NotEmpty().WithMessage("Category Type is required")
            .MustAsync(CategoryTypeExists)
            .WithMessage("Category Type not found");
    }

    private async Task<bool> CategoryExists(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _categoryRepository.Exists(categoryId);
    }

    private async Task<bool> CategoryTypeExists(int categoryTypeId, CancellationToken cancellationToken)
    {
        return await _categoryTypeRepository.Exists(categoryTypeId);
    }
}
