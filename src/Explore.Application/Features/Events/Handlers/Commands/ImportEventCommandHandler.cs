// ABOUTME: Command handler for importing external events with provenance metadata.
// ABOUTME: Uses EventImportCreate readiness profile, stores Draft status, emits no outbox messages.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.Event.Validators;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.Events.Handlers.Commands;

public sealed class ImportEventCommandHandler(
    IEventRepository eventRepository,
    IStorageObjectRepository storageObjectRepository,
    IUnitOfWork unitOfWork,
    HybridCache cache,
    IEventLifecyclePolicyProvider policyProvider,
    IEventLifecycleReadinessEvaluator readinessEvaluator) : IRequestHandler<ImportEventCommand, BaseCommandResponse<Guid>>
{
    private const string ValidationFailedCode = "event_import_validation_failed";
    private const string ReadinessFailedCode = "event_import_readiness_failed";

    public async Task<BaseCommandResponse<Guid>> Handle(ImportEventCommand command, CancellationToken cancellationToken)
    {
        var validator = new ImportEventRequestDtoValidator();
        var validation = await validator.ValidateAsync(command.Request, cancellationToken);
        if (!validation.IsValid)
        {
            return Failure(Guid.Empty, "Event import failed validation.", validation.Errors.Select(e => e.ErrorMessage), ValidationFailedCode);
        }

        ImportEventRequestDto request = command.Request;

        if (!await ImageReferenceEligibility.AreEligibleAsync(
                storageObjectRepository,
                request.TenantId,
                request.FeaturedImageId))
        {
            return Failure(
                Guid.Empty,
                "Event import failed validation.",
                ["Every image must be an active public safe-raster object in the current tenant."],
                ValidationFailedCode);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var eventEntity = new Event
            {
                Id = Guid.CreateVersion7(),
                Title = request.Title,
                Description = request.Description,
                TenantId = request.TenantId,
                ActorId = request.OwnerActorId,
                EventProvenanceTypeId = (int)EventProvenanceTypeEnum.Imported,
                ProvenanceSource = request.ProvenanceSource,
                ProvenanceExternalId = request.ProvenanceExternalId,
                EventTypeId = request.EventTypeId,
                AudienceGenderId = request.AudienceGenderId,
                AudienceAgeId = request.AudienceAgeId,
                VisibilityTypeId = request.VisibilityTypeId ?? (int)VisibilityTypeEnum.Private,
                VisibilityType = null!,
                EventFormatId = request.EventFormatId ?? (int)EventFormatEnum.Local,
                EventFormat = null!,
                Timezone = request.Timezone,
                Price = request.Price,
                FeaturedImageId = request.FeaturedImageId,
                EventStatusId = (int)EventStatusEnum.Draft,
                EventStatus = null!,
                Tenant = null!,
                Actor = null!,
                TotalViews = 0
            };

            eventEntity.ParticipationConfiguration = EventParticipationConfiguration.Create(
                eventEntity.Id,
                request.TenantId,
                request.ParticipationConfiguration.ParticipationHandlingModeId,
                request.ParticipationConfiguration.AdvanceRegistrationObligationId,
                request.ParticipationConfiguration.IdentityAccessModeId,
                request.ParticipationConfiguration.GuestRecoveryPolicy,
                DateTime.UtcNow);

            EventLifecyclePolicy policy = await policyProvider.GetEffectivePolicyAsync(request.TenantId, ValidationProfile.EventImportCreate, token);
            LifecycleReadinessResult readiness = readinessEvaluator.Evaluate(eventEntity, ValidationProfile.EventImportCreate, policy);
            if (!readiness.IsReady)
            {
                return Failure(Guid.Empty, "Event import failed readiness checks.", readiness.Errors.Select(e => e.Message), ReadinessFailedCode);
            }

            Event created = await eventRepository.Create(eventEntity);

            await cache.RemoveByTagAsync(CacheTags.EventListByTenant(created.TenantId), token);

            return Success(created.Id, "Event imported successfully.");
        }, cancellationToken);
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
