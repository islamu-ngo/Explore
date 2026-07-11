// ABOUTME: Query handler returning a single indexed DID record by ID.
// ABOUTME: Maps entity to IndexedDidDto.
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.Features.IndexedDids.Requests.Queries;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.IndexedDids.Handlers.Queries;

public class GetIndexedDidDetailsRequestHandler : IRequestHandler<GetIndexedDidDetailsRequest, IndexedDidDto?>
{
    private readonly IIndexedDidRepository _indexedDidRepository;
    private readonly IMapper _mapper;

    public GetIndexedDidDetailsRequestHandler(IIndexedDidRepository indexedDidRepository, IMapper mapper)
    {
        _indexedDidRepository = indexedDidRepository;
        _mapper = mapper;
    }

    public async Task<IndexedDidDto?> Handle(GetIndexedDidDetailsRequest request, CancellationToken cancellationToken)
    {
        var indexedDid = await _indexedDidRepository.GetIndexedDidByDid(request.Did);
        if (indexedDid == null)
        {
            return null;
        }

        return _mapper.Map<IndexedDidDto>(indexedDid);
    }
}
