// ABOUTME: Handler for all event updates using the null-check DTO pattern.
// ABOUTME: Checks which DTO is non-null on the command and applies only that specific update.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventStatusRepository _eventStatusRepository;
    private readonly IAudienceAgeRepository _audienceAgeRepository;
    private readonly IAudienceGenderRepository _audienceGenderRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;

    public UpdateEventCommandHandler(
        IEventRepository eventRepository,
        IEventStatusRepository eventStatusRepository,
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IActorRepository actorRepository,
        IStorageObjectRepository storageObjectRepository,
        IMapper mapper,
        HybridCache cache)
    {
        _eventRepository = eventRepository;
        _eventStatusRepository = eventStatusRepository;
        _audienceAgeRepository = audienceAgeRepository;
        _audienceGenderRepository = audienceGenderRepository;
        _eventTypeRepository = eventTypeRepository;
        _actorRepository = actorRepository;
        _storageObjectRepository = storageObjectRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var @event = await _eventRepository.GetById(request.Id);
        if (@event == null)
        {
            response.Success = false;
            response.Message = "Event not found.";
            return response;
        }

        if (request.EventDto is not null)
        {
            var validator = new UpdateEventDtoValidator(
                _audienceAgeRepository, _audienceGenderRepository,
                _eventTypeRepository, _actorRepository, _storageObjectRepository);
            var validationResult = await validator.ValidateAsync(request.EventDto, cancellationToken);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            _mapper.Map(request.EventDto, @event);
        }

        if (request.EventStatusDto is not null)
        {
            var validator = new UpdateEventStatusDtoValidator(_eventStatusRepository);
            var validationResult = await validator.ValidateAsync(request.EventStatusDto, cancellationToken);

            if (!validationResult.IsValid)
            {
                response.Success = false;
                response.Message = "Event status update failed.";
                response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return response;
            }

            @event.EventStatusId = request.EventStatusDto.EventStatusId;
        }

        await _eventRepository.Update(@event);

        response.Success = true;
        response.Id = @event.Id;
        response.Message = "Event updated successfully.";

        await _cache.RemoveAsync($"event:detail:{@event.Id}", cancellationToken);
        await _cache.RemoveAsync("events:list:1:20", cancellationToken);

        return response;
    }
}
