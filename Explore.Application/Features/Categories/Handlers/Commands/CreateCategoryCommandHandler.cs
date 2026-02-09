using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Category.Validators;
using Explore.Application.Features.Categories.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Categories.Handlers.Commands;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, BaseCommandResponse<Guid>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        ITenantContext tenantContext,
        IMapper mapper,
        HybridCache cache)
    {
        _categoryRepository = categoryRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateCategoryDtoValidator(_categoryRepository);
        var validationResult = await validator.ValidateAsync(request.CategoryDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Category creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var category = _mapper.Map<Category>(request.CategoryDto);

        // Set TenantId from the request context
        category.TenantId = _tenantContext.TenantId;

        category = await _categoryRepository.Create(category);

        response.Success = true;
        response.Id = category.Id;
        response.Message = "Category created successfully.";

        await _cache.RemoveAsync("categories:list:1:20", cancellationToken);

        return response;
    }
}
