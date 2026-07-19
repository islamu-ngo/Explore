// ABOUTME: Handler for creating unscheduled draft event sessions under an existing event.
// ABOUTME: Applies lifecycle readiness policy before persisting a draft without fake schedule values.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public sealed class CreateDraftEventSessionCommandHandler(
    IEventSessionRepository eventSessionRepository,
    IEventRepository eventRepository,
    IEventLifecyclePolicyProvider policyProvider,
    IEventLifecycleReadinessEvaluator readinessEvaluator,
    IUnitOfWork unitOfWork,
    EventLocationAttachmentService eventLocationAttachmentService,
    HybridCache cache) : IRequestHandler<CreateDraftEventSessionCommand, BaseCommandResponse<Guid>>
{
    private const string ReadinessFailedCode = "event_session_draft_readiness_failed";

    public async Task<BaseCommandResponse<Guid>> Handle(CreateDraftEventSessionCommand command, CancellationToken cancellationToken)
    {
        var validator = new CreateDraftEventSessionRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(command.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(Guid.Empty, "Event session draft creation failed validation.", validationResult.Errors.Select(error => error.ErrorMessage));
        }

        var parentEvent = await eventRepository.GetById(command.Request.EventId);
        if (parentEvent is null)
        {
            return Failure(Guid.Empty, "Event was not found.", ["Event was not found."]);
        }

        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = parentEvent.Id,
            Event = null!,
            TenantId = parentEvent.TenantId,
            Tenant = null!,
            EventSessionStatusId = (int)EventSessionStatusEnum.Draft,
            Title = command.Request.Title,
            Description = command.Request.Description,
            SortOrder = command.Request.SortOrder,
            CurrentAudienceAttendees = 0
        };

        EventLifecyclePolicy policy = await policyProvider.GetEffectivePolicyAsync(session.TenantId, ValidationProfile.SessionDraftCreate, cancellationToken);
        LifecycleReadinessResult readiness = readinessEvaluator.Evaluate(session, parentEvent, ValidationProfile.SessionDraftCreate, policy);
        if (!readiness.IsReady)
        {
            return Failure(Guid.Empty, "Event session draft is not ready to create.", readiness.Errors.Select(error => error.Message), ReadinessFailedCode);
        }

        EventSession created = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            EventLocation eventLocation = await eventLocationAttachmentService.ResolveAsync(
                parentEvent.Id,
                locationId: null,
                currentEventLocationId: null,
                token);
            session.AssignEventLocation(eventLocation);
            return await eventSessionRepository.Create(session);
        }, cancellationToken);

        await cache.RemoveAsync($"event:detail:{parentEvent.Id}", cancellationToken);
        await cache.RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), cancellationToken);

        return Success(created.Id, "Event session draft created successfully.");
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(Guid id, string message, IEnumerable<string> errors, string? failureCode = null) => new()
    {
        Success = false,
        Id = id,
        Message = message,
        Errors = errors.ToList(),
        FailureCode = failureCode
    };
}
