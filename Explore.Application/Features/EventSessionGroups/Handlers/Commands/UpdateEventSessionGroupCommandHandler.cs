// ABOUTME: Handler for updating event session groups without changing tenant ownership.
// ABOUTME: Validates same-event consistency and keeps sessions assigned through join entities.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionGroup.Validators;
using Explore.Application.Features.EventSessionGroups.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Handlers.Commands;

public class UpdateEventSessionGroupCommandHandler : IRequestHandler<UpdateEventSessionGroupCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly ILocationRoomRepository _locationRoomRepository;

    public UpdateEventSessionGroupCommandHandler(
        IEventSessionGroupRepository eventSessionGroupRepository,
        IEventRepository eventRepository,
        ILocationRepository locationRepository,
        ILocationRoomRepository locationRoomRepository)
    {
        _eventSessionGroupRepository = eventSessionGroupRepository;
        _eventRepository = eventRepository;
        _locationRepository = locationRepository;
        _locationRoomRepository = locationRoomRepository;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionGroupCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionGroupRequestDtoValidator(
            _eventRepository,
            _eventSessionGroupRepository,
            _locationRepository,
            _locationRoomRepository);
        var validationResult = await validator.ValidateAsync(request.EventSessionGroup, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Event session group update failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        var group = await _eventSessionGroupRepository.GetForUpdateAsync(request.EventSessionGroup.Id, cancellationToken);
        if (group is null)
        {
            response.Success = false;
            response.Message = "Event session group not found.";
            return response;
        }

        if (group.EventId != request.EventSessionGroup.EventId)
        {
            response.Success = false;
            response.Message = "Event session group does not belong to the requested event.";
            return response;
        }

        if (await SlugExistsForEventAsync(
                request.EventSessionGroup.EventId,
                request.EventSessionGroup.Slug,
                request.EventSessionGroup.Id,
                cancellationToken))
        {
            response.Success = false;
            response.Message = "Event session group update failed.";
            response.Errors = ["Slug must be unique within the event."];
            return response;
        }

        group.Name = request.EventSessionGroup.Name;
        group.Slug = request.EventSessionGroup.Slug;
        group.Description = request.EventSessionGroup.Description;
        group.LocationId = request.EventSessionGroup.LocationId;
        group.RoomId = request.EventSessionGroup.RoomId;
        group.Color = request.EventSessionGroup.Color;
        group.SortOrder = request.EventSessionGroup.SortOrder;
        group.IsPublished = request.EventSessionGroup.IsPublished;

        await _eventSessionGroupRepository.Update(group);

        response.Success = true;
        response.Id = group.Id;
        response.Message = "Event session group updated successfully.";
        return response;
    }

    private async Task<bool> SlugExistsForEventAsync(Guid eventId, string? slug, Guid currentGroupId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        var groups = await _eventSessionGroupRepository.GetActiveByEventAsync(eventId, cancellationToken);
        return groups.Any(group => group.Id != currentGroupId
            && string.Equals(group.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
