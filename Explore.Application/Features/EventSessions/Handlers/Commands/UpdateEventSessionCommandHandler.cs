using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public class UpdateEventSessionCommandHandler : IRequestHandler<UpdateEventSessionCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IRegistrationModeRepository _registrationModeRepository;
    private readonly IEventSessionIslamicAspectRepository _eventSessionIslamicAspectRepository;
    private readonly IMapper _mapper;

    public UpdateEventSessionCommandHandler(
        IEventSessionRepository eventSessionRepository,
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        IEventSessionIslamicAspectRepository eventSessionIslamicAspectRepository,
        IMapper mapper)
    {
        _eventSessionRepository = eventSessionRepository;
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
        _registrationModeRepository = registrationModeRepository;
        _eventSessionIslamicAspectRepository = eventSessionIslamicAspectRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionDtoValidator(_eventRepository, _locationRepository, _registrationModeRepository);
        var validationResult = await validator.ValidateAsync(request.EventSessionDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event session update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var eventSession = await _eventSessionRepository.GetById(request.EventSessionDto.Id);

        if (eventSession == null)
        {
            response.Success = false;
            response.Message = "Event session not found.";
            return response;
        }

        // Verify the target event belongs to the same tenant as the session (defense-in-depth)
        var parentEvent = await _eventRepository.GetById(request.EventSessionDto.EventId);
        if (parentEvent == null || parentEvent.TenantId != eventSession.TenantId)
        {
            response.Success = false;
            response.Message = "Event does not belong to the same tenant as the session.";
            return response;
        }

        _mapper.Map(request.EventSessionDto, eventSession);

        await _eventSessionRepository.Update(eventSession);

        var existingIslamicAspect = await _eventSessionIslamicAspectRepository.GetById(eventSession.Id);
        if (request.EventSessionDto.IslamicAspect == null)
        {
            if (existingIslamicAspect != null)
            {
                await _eventSessionIslamicAspectRepository.Delete(existingIslamicAspect);
            }
        }
        else if (existingIslamicAspect == null)
        {
            var newAspect = _mapper.Map<EventSessionIslamicAspect>(request.EventSessionDto.IslamicAspect);
            newAspect.EventSessionId = eventSession.Id;
            newAspect.EventSession = null;
            await _eventSessionIslamicAspectRepository.Create(newAspect);
        }
        else
        {
            _mapper.Map(request.EventSessionDto.IslamicAspect, existingIslamicAspect);
            await _eventSessionIslamicAspectRepository.Update(existingIslamicAspect);
        }

        response.Success = true;
        response.Id = eventSession.Id;
        response.Message = "Event session updated successfully.";

        return response;
    }
}
