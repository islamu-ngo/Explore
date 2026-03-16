using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSeries.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Handlers.Commands;

public class UpdateEventSeriesCommandHandler : IRequestHandler<UpdateEventSeriesCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly IMapper _mapper;

    public UpdateEventSeriesCommandHandler(IEventSeriesRepository eventSeriesRepository, IMapper mapper)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSeriesCommand request, CancellationToken cancellationToken)
    {
        var series = await _eventSeriesRepository.GetById(request.EventSeriesDto.Id);
        if (series == null)
        {
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Event series not found."
            };
        }

        _mapper.Map(request.EventSeriesDto, series);
        await _eventSeriesRepository.Update(series);

        return new BaseCommandResponse<Guid>
        {
            Id = series.Id,
            Success = true,
            Message = "Event series updated successfully."
        };
    }
}
