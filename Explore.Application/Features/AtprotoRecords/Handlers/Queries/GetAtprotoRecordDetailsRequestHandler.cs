// ABOUTME: Query handler returning a single AT Protocol record by ID.
// ABOUTME: Maps entity to AtprotoRecordDto via AutoMapper.
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Features.AtprotoRecords.Requests.Queries;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Handlers.Queries;

public class GetAtprotoRecordDetailsRequestHandler : IRequestHandler<GetAtprotoRecordDetailsRequest, AtprotoRecordDto?>
{
    private readonly IAtprotoRecordRepository _atprotoRecordRepository;
    private readonly IMapper _mapper;

    public GetAtprotoRecordDetailsRequestHandler(IAtprotoRecordRepository atprotoRecordRepository, IMapper mapper)
    {
        _atprotoRecordRepository = atprotoRecordRepository;
        _mapper = mapper;
    }

    public async Task<AtprotoRecordDto?> Handle(GetAtprotoRecordDetailsRequest request, CancellationToken cancellationToken)
    {
        var atprotoRecord = await _atprotoRecordRepository.GetById(request.Id);
        if (atprotoRecord == null)
        {
            return null;
        }

        return _mapper.Map<AtprotoRecordDto>(atprotoRecord);
    }
}
