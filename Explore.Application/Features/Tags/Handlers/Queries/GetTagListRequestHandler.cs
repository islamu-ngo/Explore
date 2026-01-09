using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Tag;
using Explore.Application.Features.Tags.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Tags.Handlers.Queries
{
    public class GetTagListRequestHandler : IRequestHandler<GetTagListRequest, List<TagListDto>>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;

        public GetTagListRequestHandler(
            ITagRepository tagRepository,
            IMapper mapper)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
        }

        public async Task<List<TagListDto>> Handle(GetTagListRequest request, CancellationToken cancellationToken)
        {
            var tags = await _tagRepository.GetTagsWithDetails();
            return _mapper.Map<List<TagListDto>>(tags);
        }
    }
}
