// ABOUTME: Creates a tenant-scoped public action after authorization and URL validation.
// ABOUTME: New organizer-managed destinations enter pending review and enforce one primary action.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventPublicAction.Validators;
using Explore.Application.Features.EventPublicActions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using Explore.Domain.ValueObjects;
using MediatR;

namespace Explore.Application.Features.EventPublicActions.Handlers.Commands;

public sealed class CreateEventPublicActionCommandHandler(
    IEventRepository eventRepository,
    IEventPublicActionRepository actionRepository,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateEventPublicActionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        CreateEventPublicActionCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new ManageEventPublicActionDtoValidator(requireConcurrencyStamp: false);
        var validation = await validator.ValidateAsync(request.Action, cancellationToken);
        if (!validation.IsValid)
        {
            return Failure("Public action failed validation.", validation.Errors.Select(error => error.ErrorMessage));
        }

        if (currentUserService.UserId is null)
        {
            return Failure("Public action could not be created.", ["An authenticated user is required."]);
        }

        var @event = await eventRepository.GetAuthorizationTargetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != tenantContext.TenantId)
        {
            return Failure("Public action could not be created.", ["Event was not found in the current tenant."]);
        }

        if (@event.ParticipationConfiguration is null
            || !EventAuthorityRules.IsPublicActionAllowed(
                @event.ParticipationConfiguration.ParticipationHandlingModeId,
                request.Action.KindId))
        {
            return Failure(
                "Public action could not be created.",
                ["Public action kind is not available for this event's participation mode."]);
        }

        if (request.Action.IsPrimary
            && await actionRepository.HasOtherPrimaryAsync(request.EventId, excludedActionId: null, cancellationToken))
        {
            return Failure("Public action could not be created.", ["The event already has a primary public action."]);
        }

        var action = new EventPublicAction
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantContext.TenantId,
            EventId = request.EventId,
            EventPublicActionKindId = request.Action.KindId,
            HealthStateId = (int)EventPublicActionHealthStateEnum.PendingReview,
            Label = NormalizeOptional(request.Action.Label),
            SortOrder = request.Action.SortOrder,
            IsPrimary = request.Action.IsPrimary
        };
        action.SetDestination(ExternalActionUrl.Create(request.Action.Url));

        var created = await actionRepository.Create(action);
        return Success(created.Id, "Public action created pending review.");
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(string message, IEnumerable<string> errors) => new()
    {
        Success = false,
        Message = message,
        Errors = errors.ToList()
    };
}
