// ABOUTME: Creates organizer claims only for actors controlled by the authenticated user.
// ABOUTME: Serializable replay detection returns the existing claim for retry-idempotent submissions.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventOrganizerClaim.Validators;
using Explore.Application.Features.EventOrganizerClaims.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.EventOrganizerClaims.Handlers.Commands;

public sealed class SubmitEventOrganizerClaimCommandHandler(
    IEventRepository eventRepository,
    IEventOrganizerClaimRepository claimRepository,
    IActorRepository actorRepository,
    ITenantUserRepository tenantUserRepository,
    IOrganizationTenantRepository organizationTenantRepository,
    IGroupTenantRepository groupTenantRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IGroupMemberRepository groupMemberRepository,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<SubmitEventOrganizerClaimCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        SubmitEventOrganizerClaimCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new SubmitEventOrganizerClaimDtoValidator();
        var validation = await validator.ValidateAsync(request.Claim, cancellationToken);
        if (!validation.IsValid)
        {
            return Failure("Organizer claim failed validation.", validation.Errors.Select(error => error.ErrorMessage));
        }

        if (currentUserService.UserId is not { } userId)
        {
            return Failure("Organizer claim could not be submitted.", ["An authenticated user is required."]);
        }

        var @event = await eventRepository.GetAuthorizationTargetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != tenantContext.TenantId)
        {
            return Failure("Organizer claim could not be submitted.", ["Event was not found in the current tenant."]);
        }

        if (!await ClaimantActorAccessEvaluator.CanControlAsync(
                request.Claim.ClaimantActorId,
                userId,
                tenantContext.TenantId,
                actorRepository,
                tenantUserRepository,
                organizationTenantRepository,
                groupTenantRepository,
                organizationMemberRepository,
                groupMemberRepository,
                cancellationToken))
        {
            return Failure("Organizer claim could not be submitted.", ["The claimant actor is not controlled by the current user."]);
        }

        var pendingClaim = EventOrganizerClaim.CreatePending(
            tenantContext.TenantId,
            request.EventId,
            request.Claim.ClaimantActorId,
            request.Claim.EvidenceType,
            request.Claim.EvidenceReference,
            DateTime.UtcNow);
        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            var existing = await claimRepository.GetByEventAndClaimantAsync(
                request.EventId,
                request.Claim.ClaimantActorId,
                trackChanges: false,
                token);
            if (existing is not null)
            {
                return Success(existing.Id, "Organizer claim already exists.");
            }

            var created = await claimRepository.Create(pendingClaim);
            return Success(created.Id, "Organizer claim submitted.");
        }, cancellationToken);
    }

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
