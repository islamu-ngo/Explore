using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Category.Validators;
using Explore.Application.Features.Categories.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Categories.Handlers.Commands;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, BaseCommandResponse<Guid>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IMapper mapper,
        HybridCache cache)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateCategoryDtoValidator(_categoryRepository);
        var validationResult = await validator.ValidateAsync(request.CategoryDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Category update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var category = await _categoryRepository.GetById(request.CategoryDto.Id);

        if (category == null)
        {
            response.Success = false;
            response.Message = "Category not found.";
            return response;
        }

        _mapper.Map(request.CategoryDto, category);

        await _categoryRepository.Update(category);

        response.Success = true;
        response.Id = category.Id;
        response.Message = "Category updated successfully.";

        await _cache.RemoveAsync("categories:list:1:20", cancellationToken);

        return response;
    }
}
