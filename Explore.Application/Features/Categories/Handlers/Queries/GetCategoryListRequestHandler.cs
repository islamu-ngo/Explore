using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Category;
using Explore.Application.Features.Categories.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Categories.Handlers.Queries
{
    public class GetCategoryListRequestHandler : IRequestHandler<GetCategoryListRequest, PaginatedResult<CategoryListDto>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;

        public GetCategoryListRequestHandler(
            ICategoryRepository categoryRepository,
            IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<CategoryListDto>> Handle(GetCategoryListRequest request, CancellationToken cancellationToken)
        {
            var (pageNumber, pageSize) = PaginatedResult<CategoryListDto>.NormalizeParameters(request.PageNumber, request.PageSize);
            var (categories, totalCount) = await _categoryRepository.GetCategoriesWithDetailsPaged(pageNumber, pageSize);
            var dtos = _mapper.Map<List<CategoryListDto>>(categories);
            return PaginatedResult<CategoryListDto>.Create(dtos, totalCount, pageNumber, pageSize);
        }
    }
}
