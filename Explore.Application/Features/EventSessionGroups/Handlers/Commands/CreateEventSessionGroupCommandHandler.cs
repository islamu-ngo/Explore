// ABOUTME: Handler for creating event session groups used as tracks, devrooms, stages, or program sections.
// ABOUTME: Derives TenantId from the parent Event and validates location/room references via tenant-filtered repositories.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionGroup.Validators;
using Explore.Application.Features.EventSessionGroups.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Handlers.Commands;

public class CreateEventSessionGroupCommandHandler : IRequestHandler<CreateEventSessionGroupCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;
    private readonly IMapper _mapper;

    public CreateEventSessionGroupCommandHandler(
        IEventSessionGroupRepository eventSessionGroupRepository,
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        ILocationRoomRepository locationRoomRepository,
        IMapper mapper)
    {
        _eventSessionGroupRepository = eventSessionGroupRepository;
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
        _locationRoomRepository = locationRoomRepository;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventSessionGroupCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new CreateEventSessionGroupRequestDtoValidator(
            _eventRepository,
            _locationRepository,
            _locationRoomRepository);
        var validationResult = await validator.ValidateAsync(request.EventSessionGroup, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event session group creation failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        var parentEvent = await _eventRepository.GetById(request.EventSessionGroup.EventId);
        if (parentEvent is null)
        {
            response.Success = false;
            response.Message = "Event not found in the current tenant.";
            return response;
        }

        if (await SlugExistsForEventAsync(
                request.EventSessionGroup.EventId,
                request.EventSessionGroup.Slug,
                cancellationToken))
        {
            response.Success = false;
            response.Message = "Event session group creation failed.";
            response.Errors = ["Slug must be unique within the event."];
            return response;
        }

        var group = _mapper.Map<EventSessionGroup>(request.EventSessionGroup);
        group.TenantId = parentEvent.TenantId;

        group = await _eventSessionGroupRepository.Create(group);

        response.Success = true;
        response.Id = group.Id;
        response.Message = "Event session group created successfully.";
        return response;
    }

    private async Task<bool> SlugExistsForEventAsync(Guid eventId, string? slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        var groups = await _eventSessionGroupRepository.GetActiveByEventAsync(eventId, cancellationToken);
        return groups.Any(group => string.Equals(group.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
