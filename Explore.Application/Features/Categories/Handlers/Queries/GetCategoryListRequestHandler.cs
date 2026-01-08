using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Category;
using Explore.Application.Features.Categories.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Categories.Handlers.Queries
{
    public class GetCategoryListRequestHandler : IRequestHandler<GetCategoryListRequest, List<CategoryListDto>>
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

        public async Task<List<CategoryListDto>> Handle(GetCategoryListRequest request, CancellationToken cancellationToken)
        {
            var categories = await _categoryRepository.GetCategoriesWithDetails();
            return _mapper.Map<List<CategoryListDto>>(categories);
        }
    }
}
