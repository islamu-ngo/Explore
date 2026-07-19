// ABOUTME: Applies an optimistic EventLocation disclosure-policy change with append-only audit evidence.
// ABOUTME: Commits the aggregate and PII-free audit atomically before evicting projection cache tags.

using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventLocations.Requests.Commands;
using Explore.Application.Features.EventLocations.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventLocations.Handlers.Commands;

public sealed class UpdateEventLocationPolicyCommandHandler(
    IEventLocationRepository eventLocations,
    IEventLocationDisclosureAuditRepository audits,
    IUnitOfWork unitOfWork,
    HybridCache cache,
    ITenantContext tenantContext,
    IUserContext userContext,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateEventLocationPolicyCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateEventLocationPolicyCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new UpdateEventLocationPolicyCommandValidator();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Failure(
                request.EventLocationId,
                "event_location_policy_validation_failed",
                "EventLocation policy update failed validation.",
                validation.Errors.Select(error => error.ErrorMessage));
        }

        Guid actorUserId = userContext.GetRequiredUserId();
        BaseCommandResponse<Guid> result = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            EventLocation? eventLocation = await eventLocations.GetForUpdateAsync(
                request.EventLocationId,
                token);
            if (eventLocation is null
                || eventLocation.TenantId != tenantContext.TenantId
                || eventLocation.EventId != request.EventId)
            {
                return Failure(
                    request.EventLocationId,
                    "event_location_policy_not_found",
                    "EventLocation policy was not found.");
            }

            if (eventLocation.ConcurrencyStamp != request.ExpectedConcurrencyStamp
                || eventLocation.PolicyVersion != request.ExpectedPolicyVersion)
            {
                throw ConcurrencyConflict(eventLocation.Id);
            }

            if (eventLocation.IsToBeAnnounced
                && (request.SelectedFields != EventLocationDisclosureFields.None
                    || request.FullDetailsAudience != LocationDisclosureAudienceEnum.Never
                    || request.RevealFullDetailsFromUtc.HasValue))
            {
                return Failure(
                    eventLocation.Id,
                    "event_location_policy_tba_fields_forbidden",
                    "A to-be-announced EventLocation cannot disclose physical-location fields.");
            }

            EventLocationDisclosureAudit audit = eventLocation.ChangeDisclosurePolicy(
                request.SelectedFields,
                request.FullDetailsAudience,
                request.RevealFullDetailsFromUtc,
                request.ExpectedPolicyVersion,
                actorUserId,
                EventLocationDisclosureAuditReasonEnum.OrganizerPolicyChange,
                timeProvider.GetUtcNow().UtcDateTime,
                request.NeedsPrivacyReview);
            await audits.AppendAsync(audit, token);
            return Success(eventLocation.Id);
        }, cancellationToken);

        if (!result.Success)
        {
            return result;
        }

        await cache.RemoveByTagAsync(
            CacheTags.EventLocation(request.EventLocationId),
            CancellationToken.None);
        return result;
    }

    private static ConcurrencyConflictException ConcurrencyConflict(Guid eventLocationId) => new(
        ConcurrencyConflictException.ConcurrentUpdate,
        "The EventLocation disclosure policy was modified by another request. Reload and retry.",
        nameof(EventLocation),
        eventLocationId.ToString("D"));

    private static BaseCommandResponse<Guid> Success(Guid eventLocationId) => new()
    {
        Id = eventLocationId,
        Success = true,
        Message = "EventLocation policy updated."
    };

    private static BaseCommandResponse<Guid> Failure(
        Guid eventLocationId,
        string code,
        string message,
        IEnumerable<string>? errors = null) => new()
    {
        Id = eventLocationId,
        Success = false,
        FailureCode = code,
        Message = message,
        Errors = errors?.ToList() ?? [message]
    };
}
