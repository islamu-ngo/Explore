// ABOUTME: Applies grouped Application-only updates to category-to-category-type junctions.
// ABOUTME: Enforces persisted tenant ownership and duplicate-pair rejection before one save.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CategoryTypeCategories.Validators;
using Explore.Application.Features.CategoryTypeCategories.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Handlers.Commands;

public class UpdateCategoryTypeCategoriesCommandHandler : IRequestHandler<UpdateCategoryTypeCategoriesCommand, BaseCommandResponse<Guid>>
{
    private readonly ICategoryTypeCategoriesRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICategoryTypeRepository _categoryTypeRepository;
    private readonly ITenantContext _tenantContext;

    public UpdateCategoryTypeCategoriesCommandHandler(
        ICategoryTypeCategoriesRepository repository,
        ICategoryRepository categoryRepository,
        ICategoryTypeRepository categoryTypeRepository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _categoryTypeRepository = categoryTypeRepository;
        _tenantContext = tenantContext;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateCategoryTypeCategoriesCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateCategoryTypeCategoriesDtoValidator();
        var validationResult = await validator.ValidateAsync(request.CategoryTypeCategoriesDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Category Type Categories update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var link = await _repository.GetById(request.CategoryTypeCategoriesId);
        if (link == null || link.TenantId != _tenantContext.TenantId)
        {
            response.Success = false;
            response.Message = "Category Type Categories not found.";
            return response;
        }

        Guid categoryId = request.CategoryTypeCategoriesDto.Relationship?.CategoryId ?? link.CategoryId;
        int categoryTypeId = request.CategoryTypeCategoriesDto.Relationship?.CategoryTypeId ?? link.CategoryTypeId;
        var category = await _categoryRepository.GetById(categoryId);
        if (category is null || category.TenantId != link.TenantId || !await _categoryTypeRepository.Exists(categoryTypeId))
        {
            response.Success = false;
            response.Message = "Category Type Categories update failed.";
            response.Errors = ["Relationship targets were not found in the current tenant."];
            return response;
        }

        if ((categoryId != link.CategoryId || categoryTypeId != link.CategoryTypeId)
            && await _repository.Exists(categoryId, categoryTypeId))
        {
            response.Success = false;
            response.Message = "Category Type Categories update failed.";
            response.Errors = ["Category and Category Type relationship already exists."];
            return response;
        }

        link.CategoryId = categoryId;
        link.CategoryTypeId = categoryTypeId;
        await _repository.Update(link);

        response.Success = true;
        response.Id = link.Id;
        response.Message = "Category Type Categories updated successfully.";

        return response;
    }
}
