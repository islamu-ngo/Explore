// ABOUTME: Handler for updating a category-to-category-type link record with validation.
// ABOUTME: Validates input, fetches entity, applies updates.
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.CategoryTypeCategories.Validators;
using Explore.Application.Features.CategoryTypeCategories.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.CategoryTypeCategories.Handlers.Commands;

public class UpdateCategoryTypeCategoriesCommandHandler : IRequestHandler<UpdateCategoryTypeCategoriesCommand, BaseCommandResponse<Guid>>
{
    private readonly ICategoryTypeCategoriesRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICategoryTypeRepository _categoryTypeRepository;

    public UpdateCategoryTypeCategoriesCommandHandler(
        ICategoryTypeCategoriesRepository repository,
        IMapper mapper,
        ICategoryRepository categoryRepository,
        ICategoryTypeRepository categoryTypeRepository)
    {
        _repository = repository;
        _mapper = mapper;
        _categoryRepository = categoryRepository;
        _categoryTypeRepository = categoryTypeRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateCategoryTypeCategoriesCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateCategoryTypeCategoriesDtoValidator(_categoryRepository, _categoryTypeRepository);
        var validationResult = await validator.ValidateAsync(request.CategoryTypeCategoriesDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Category Type Categories update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var categoryTypeCategories = await _repository.GetById(request.CategoryTypeCategoriesDto.Id);
        if (categoryTypeCategories == null)
        {
            response.Success = false;
            response.Message = "Category Type Categories not found.";
            return response;
        }

        _mapper.Map(request.CategoryTypeCategoriesDto, categoryTypeCategories);
        await _repository.Update(categoryTypeCategories);

        response.Success = true;
        response.Id = categoryTypeCategories.Id;
        response.Message = "Category Type Categories updated successfully.";

        return response;
    }
}
