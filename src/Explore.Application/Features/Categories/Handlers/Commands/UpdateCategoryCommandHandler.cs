// ABOUTME: Handler for grouped Category PATCH updates with optimistic concurrency.
// ABOUTME: Validates groups, loads the entity once, applies explicit field updates, and invalidates category caches after save.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.Category.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.Categories.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Categories.Handlers.Commands;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, BaseCommandResponse<Guid>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly HybridCache _cache;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        HybridCache cache)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateCategoryDtoValidator(request.CategoryId, _categoryRepository);
        var validationResult = await validator.ValidateAsync(request.UpdateCategoryDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Category update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var category = await _categoryRepository.GetById(request.CategoryId);

        if (category == null)
        {
            response.Success = false;
            response.Message = "Category not found.";
            return response;
        }

        if (category.ConcurrencyStamp != request.ExpectedConcurrencyStamp)
        {
            throw new ConcurrencyConflictException(
                ConcurrencyConflictException.ConcurrentUpdate,
                "The category was modified by another request. Reload and retry.",
                nameof(Category),
                category.Id.ToString());
        }

        ApplyUpdates(category, request.UpdateCategoryDto);

        await _categoryRepository.Update(category);

        response.Success = true;
        response.Id = category.Id;
        response.Message = "Category updated successfully.";

        await _cache.RemoveAsync("categories:list:1:20", cancellationToken);

        return response;
    }

    private static void ApplyUpdates(Category category, UpdateCategoryDto dto)
    {
        if (dto.MasterCode is not null)
        {
            category.MasterCode = dto.MasterCode.Value;
        }

        if (dto.FullName is not null)
        {
            category.FullName = dto.FullName.Value;
        }

        if (dto.Parent is { ParentId.HasValue: true })
        {
            category.ParentId = dto.Parent.ParentId.Value;
        }
    }
}
