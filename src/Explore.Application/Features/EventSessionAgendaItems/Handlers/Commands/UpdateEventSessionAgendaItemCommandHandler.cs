// ABOUTME: Handler for updating an event session agenda item with validation.
// ABOUTME: Validates input, fetches entity, applies updates.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem.Validators;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Handlers.Commands;

public class UpdateEventSessionAgendaItemCommandHandler : IRequestHandler<UpdateEventSessionAgendaItemCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSessionAgendaItemRepository _agendaItemRepository;
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;
    private readonly IMapper _mapper;

    public UpdateEventSessionAgendaItemCommandHandler(
        IEventSessionAgendaItemRepository agendaItemRepository,
        IEventSessionRepository eventSessionRepository,
        ILocationRepository locationRepository,
        IUnitOfWork unitOfWork,
        EventLocationAttachmentService eventLocationAttachmentService,
        IMapper mapper)
    {
        _agendaItemRepository = agendaItemRepository;
        _eventSessionRepository = eventSessionRepository;
        _locationRepository = locationRepository;
        _unitOfWork = unitOfWork;
        _eventLocationAttachmentService = eventLocationAttachmentService;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateEventSessionAgendaItemCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var validator = new UpdateEventSessionAgendaItemDtoValidator(_eventSessionRepository, _locationRepository);
        var validationResult = await validator.ValidateAsync(request.AgendaItemDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Agenda item update failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var agendaItem = await _agendaItemRepository.GetById(request.AgendaItemDto.Id);

        if (agendaItem == null)
        {
            response.Success = false;
            response.Message = "Agenda item not found.";
            return response;
        }

        EventSession? parentSession = await _eventSessionRepository.GetById(request.AgendaItemDto.EventSessionId);
        if (parentSession is null || parentSession.TenantId != agendaItem.TenantId)
        {
            response.Success = false;
            response.Message = "Event session not found in the current tenant.";
            return response;
        }

        Guid? previousEventLocationId = agendaItem.EventLocationId;
        _mapper.Map(request.AgendaItemDto, agendaItem);
        agendaItem.TenantId = parentSession.TenantId;
        agendaItem.EventSession = parentSession;

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            EventLocation eventLocation = await _eventLocationAttachmentService.ResolveAsync(
                parentSession.EventId,
                agendaItem.LocationId,
                previousEventLocationId,
                token);
            agendaItem.AssignEventLocation(eventLocation);
            await _agendaItemRepository.Update(agendaItem);
            await _eventLocationAttachmentService.DetachIfUnreferencedAsync(previousEventLocationId, token);
        }, cancellationToken);

        response.Success = true;
        response.Id = agendaItem.Id;
        response.Message = "Agenda item updated successfully.";

        return response;
    }
}
