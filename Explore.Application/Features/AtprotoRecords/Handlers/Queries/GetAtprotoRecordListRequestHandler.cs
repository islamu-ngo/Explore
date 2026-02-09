using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.AtprotoRecord;
using Explore.Application.Features.AtprotoRecords.Requests.Queries;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.AtprotoRecords.Handlers.Queries;

public class GetAtprotoRecordListRequestHandler : IRequestHandler<GetAtprotoRecordListRequest, List<AtprotoRecordListDto>>
{
    private readonly IAtprotoRecordRepository _atprotoRecordRepository;
    private readonly IMapper _mapper;

    public GetAtprotoRecordListRequestHandler(IAtprotoRecordRepository atprotoRecordRepository, IMapper mapper)
    {
        _atprotoRecordRepository = atprotoRecordRepository;
        _mapper = mapper;
    }

    public async Task<List<AtprotoRecordListDto>> Handle(GetAtprotoRecordListRequest request, CancellationToken cancellationToken)
    {
        var atprotoRecords = await _atprotoRecordRepository.GetAllAtprotoRecords();
        return _mapper.Map<List<AtprotoRecordListDto>>(atprotoRecords);
    }
}
