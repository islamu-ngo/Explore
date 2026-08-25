// ABOUTME: Applies grouped route-ID updates to session agenda items.
// ABOUTME: Rejects cross-event moves and invalidates the owning event cache after one transactional write.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem.Validators;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessionAgendaItems.Handlers.Commands;

public class UpdateEventSessionAgendaItemCommandHandler : IRequestHandler<UpdateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionAgendaItemRepository _agendaItemRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;
    private readonly HybridCache _cache;

    public UpdateEventSessionAgendaItemCommandHandler(
        IEventSessionAgendaItemRepository agendaItemRepository,
        IEventSessionRepository eventSessionRepository,
        ILocationRepository locationRepository,
        IUnitOfWork unitOfWork,
        EventLocationAttachmentService eventLocationAttachmentService,
        HybridCache cache)
    {
        _agendaItemRepository = agendaItemRepository;
        _eventSessionRepository = eventSessionRepository;
        _locationRepository = locationRepository;
        _unitOfWork = unitOfWork;
        _eventLocationAttachmentService = eventLocationAttachmentService;
        _cache = cache;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionAgendaItemCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionAgendaItemDtoValidator();
        var validationResult = await validator.ValidateAsync(request.AgendaItemDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Agenda item update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var agendaItem = await _agendaItemRepository.GetById(request.EventSessionAgendaItemId);

        if (agendaItem == null)
        {
            response.Success = false;
            response.Message = "Agenda item not found.";
            return response;
        }

        EventSession? currentSession = await _eventSessionRepository.GetById(agendaItem.EventSessionId);
        Guid destinationSessionId = request.AgendaItemDto.Relationship?.EventSessionId ?? agendaItem.EventSessionId;
        EventSession? parentSession = destinationSessionId == currentSession?.Id
            ? currentSession
            : await _eventSessionRepository.GetById(destinationSessionId);
        if (currentSession is null || parentSession is null || parentSession.TenantId != agendaItem.TenantId)
        {
            response.Success = false;
            response.Message = "Event session not found in the current tenant.";
            return response;
        }

        request = request with
        {
            EventSessionId = agendaItem.EventSessionId,
            EventId = currentSession.EventId,
            TenantId = agendaItem.TenantId,
        };

        if (parentSession.EventId != currentSession.EventId)
        {
            response.Success = false;
            response.Message = "Agenda items cannot move to a session from another event.";
            return response;
        }

        string title = request.AgendaItemDto.Content?.Title ?? agendaItem.Title;
        DateTimeOffset startTime = request.AgendaItemDto.Schedule?.StartTime ?? agendaItem.StartTime;
        DateTimeOffset endTime = request.AgendaItemDto.Schedule?.EndTime ?? agendaItem.EndTime;
        if (string.IsNullOrWhiteSpace(title) || endTime <= startTime)
        {
            response.Success = false;
            response.Message = "Agenda item update failed.";
            response.Errors = [string.IsNullOrWhiteSpace(title)
                ? "Title is required."
                : "EndTime must be after StartTime."];
            return response;
        }

        Guid? locationId = request.AgendaItemDto.Location?.Value.HasValue == true
            ? request.AgendaItemDto.Location.Value.Value
            : agendaItem.LocationId;
        if (locationId.HasValue)
        {
            Location? location = await _locationRepository.GetById(locationId.Value);
            if (location is null || location.TenantId != agendaItem.TenantId)
            {
                response.Success = false;
                response.Message = "Location was not found in the current tenant.";
                return response;
            }
        }

        Guid? previousEventLocationId = agendaItem.EventLocationId;
        agendaItem.EventSessionId = parentSession.Id;
        agendaItem.Title = title;
        agendaItem.StartTime = startTime;
        agendaItem.EndTime = endTime;
        if (request.AgendaItemDto.Content?.Description.HasValue == true)
            agendaItem.Description = request.AgendaItemDto.Content.Description.Value;
        agendaItem.TenantId = parentSession.TenantId;
        agendaItem.EventSession = parentSession;

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            EventLocation eventLocation = await _eventLocationAttachmentService.ResolveAsync(
                parentSession.EventId,
                locationId,
                previousEventLocationId,
                token);
            agendaItem.AssignEventLocation(eventLocation);
            await _agendaItemRepository.Update(agendaItem);
            await _eventLocationAttachmentService.DetachIfUnreferencedAsync(previousEventLocationId, token);
        }, cancellationToken);

        await _cache.RemoveAsync($"event:detail:{parentSession.EventId}", cancellationToken);
        await _cache.RemoveByTagAsync(CacheTags.EventListByTenant(parentSession.TenantId), cancellationToken);

        response.Success = true;
        response.Id = agendaItem.Id;
        response.Message = "Agenda item updated successfully.";

        return response;
    }
}
