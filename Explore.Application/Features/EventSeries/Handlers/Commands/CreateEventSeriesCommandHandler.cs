using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSeries.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSeries.Handlers.Commands;

public class CreateEventSeriesCommandHandler : IRequestHandler<CreateEventSeriesCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IMapper _mapper;

    public CreateEventSeriesCommandHandler(IEventSeriesRepository eventSeriesRepository, IMapper mapper)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventSeriesCommand request, CancellationToken cancellationToken)
    {
        var series = _mapper.Map<Domain.EventSeries>(request.EventSeriesDto);
        series = await _eventSeriesRepository.Create(series);

        return new BaseCommandResponse<Guid>
        {
            Id = series.Id,
            Success = true,
            Message = "Event series created successfully."
        };
    }
}
