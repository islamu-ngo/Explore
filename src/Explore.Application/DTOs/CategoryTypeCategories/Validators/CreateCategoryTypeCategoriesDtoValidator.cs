using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.CategoryTypeCategories.Validators;

public class CreateCategoryTypeCategoriesDtoValidator : AbstractValidator<CreateCategoryTypeCategoriesDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICategoryTypeRepository _categoryTypeRepository;
    private readonly ICategoryTypeCategoriesRepository _categoryTypeCategoriesRepository;

    public CreateCategoryTypeCategoriesDtoValidator(
        ICategoryRepository categoryRepository,
        ICategoryTypeRepository categoryTypeRepository,
        ICategoryTypeCategoriesRepository categoryTypeCategoriesRepository)
    {
        _categoryRepository = categoryRepository;
        _categoryTypeRepository = categoryTypeRepository;
        _categoryTypeCategoriesRepository = categoryTypeCategoriesRepository;

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required")
            .MustAsync(CategoryExists)
            .WithMessage("Category not found");

        RuleFor(x => x.CategoryTypeId)
            .NotEmpty().WithMessage("Category Type is required")
            .MustAsync(CategoryTypeExists)
            .WithMessage("Category Type not found");

        RuleFor(x => x)
            .MustAsync(CategoryTypeCategoryNotExist)
            .WithMessage("This Category is already assigned to this Category Type");
    }

    private async Task<bool> CategoryExists(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _categoryRepository.Exists(categoryId);
    }

    private async Task<bool> CategoryTypeExists(int categoryTypeId, CancellationToken cancellationToken)
    {
        return await _categoryTypeRepository.Exists(categoryTypeId);
    }

    private async Task<bool> CategoryTypeCategoryNotExist(CreateCategoryTypeCategoriesDto dto, CancellationToken cancellationToken)
    {
        return !await _categoryTypeCategoriesRepository.Exists(dto.CategoryId, dto.CategoryTypeId);
    }
}
