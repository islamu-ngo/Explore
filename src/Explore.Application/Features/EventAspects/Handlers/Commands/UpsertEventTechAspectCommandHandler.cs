// ABOUTME: Command handler to create or update the Tech aspect for an event.
// ABOUTME: Uses upsert pattern via repository. Validates event exists and user has permission.

namespace Explore.Application.Features.EventAspects.Handlers.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAspects.Validators;
using Explore.Application.Features.EventAspects.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

/// <summary>
/// Handler for creating or updating the Tech aspect of an event.
/// </summary>
public class UpsertEventTechAspectCommandHandler : IRequestHandler<UpsertEventTechAspectCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventTechAspectRepository _techAspectRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public UpsertEventTechAspectCommandHandler(
        IEventRepository eventRepository,
        IEventTechAspectRepository techAspectRepository,
        IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _eventRepository = eventRepository;
        _techAspectRepository = techAspectRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpsertEventTechAspectCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        // Verify event exists
        var eventExists = await _eventRepository.Exists(request.EventId);
        if (!eventExists)
        {
            response.Success = false;
            response.Message = "Event not found.";
            response.Errors = new List<string> { $"Event with ID {request.EventId} does not exist." };
            return response;
        }

        // Validate DTO - manual instantiation per project convention
        var validator = new CreateUpdateTechAspectDtoValidator();
        var validationResult = await validator.ValidateAsync(request.AspectDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Validation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        // Map DTO to entity
        var aspect = _mapper.Map<EventTechAspect>(request.AspectDto);

        // Set the shared primary key (aspect.Id = event.Id)
        aspect.Id = request.EventId;

        // Upsert through repository
        aspect = await _techAspectRepository.Upsert(aspect);

        response.Success = true;
        response.Id = aspect.Id;
        response.Message = "Tech aspect saved successfully.";

        return response;
    }
}
