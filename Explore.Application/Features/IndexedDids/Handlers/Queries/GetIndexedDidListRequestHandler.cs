using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.IndexedDid;
using Explore.Domain;
using Explore.Application.Features.IndexedDids.Requests.Queries;

namespace Explore.Application.Features.IndexedDids.Handlers.Queries
{
    public class GetIndexedDidListRequestHandler : IRequestHandler<GetIndexedDidListRequest, List<IndexedDidListDto>>
    {
        private readonly IIndexedDidRepository _indexedDidRepository;
        private readonly IMapper _mapper;

        public GetIndexedDidListRequestHandler(IIndexedDidRepository indexedDidRepository, IMapper mapper)
        {
            _indexedDidRepository = indexedDidRepository;
            _mapper = mapper;
        }

        public async Task<List<IndexedDidListDto>> Handle(GetIndexedDidListRequest request, CancellationToken cancellationToken)
        {
            var indexedDids = await _indexedDidRepository.GetAllIndexedDids();
            return _mapper.Map<List<IndexedDidListDto>>(indexedDids);
        }
    }
}
