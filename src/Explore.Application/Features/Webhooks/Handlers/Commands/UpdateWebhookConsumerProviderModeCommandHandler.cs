// ABOUTME: Applies governed webhook consumer provider-mode transitions with frozen pending-work semantics.
// ABOUTME: Validates target capabilities and bindings, records a safe audit, and uses optimistic concurrency.

using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Lookups;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class UpdateWebhookConsumerProviderModeCommandHandler(
    IWebhookConsumerRepository consumerRepository,
    IWebhookEndpointRepository endpointRepository,
    IWebhookProviderPublicationRepository providerPublicationRepository,
    IWebhookProviderCapabilityResolver capabilityResolver,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IMachinePrincipalAccessor machinePrincipalAccessor,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateWebhookConsumerProviderModeCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateWebhookConsumerProviderModeCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return Failure("webhook_consumer_provider_mode_validation_failed", validationErrors);
        }

        if (!TryResolveActor(out var actorReference, out var actorUserId))
        {
            return Failure(
                "webhook_consumer_provider_mode_actor_required",
                ["An authenticated operator identity is required."]);
        }

        var targetMode = (WebhookProviderMode)request.ProviderModeId;
        var pendingWorkDecision = (WebhookPendingWorkDecision)request.PendingWorkDecisionId;
        if (pendingWorkDecision == WebhookPendingWorkDecision.MigrateEligible)
        {
            return Failure(
                "webhook_consumer_provider_mode_pending_migration_unsupported",
                ["Provider-mode changes cannot migrate existing work between Local targets and provider publications. Preserve existing work and let new work use the new mode."]);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var consumer = await consumerRepository.GetByTenantAndIdForUpdateAsync(
                request.TenantId,
                request.ConsumerId,
                transactionCancellationToken);
            if (consumer is null || consumer.Status == WebhookConsumerStatus.Archived)
            {
                return Failure("webhook_consumer_not_found", ["Webhook consumer was not found."]);
            }

            if (consumer.ConfigurationVersion != request.ExpectedConfigurationVersion)
            {
                return Failure(
                    "webhook_consumer_configuration_conflict",
                    ["Webhook consumer configuration changed. Reload it before applying another update."]);
            }

            if (consumer.ProviderMode == targetMode)
            {
                return Failure(
                    "webhook_consumer_provider_mode_unchanged",
                    ["Webhook consumer already uses the requested provider mode."]);
            }

            var capability = capabilityResolver.Resolve(targetMode);
            if (!capability.IsProviderModeAvailable)
            {
                return Failure(
                    "webhook_consumer_provider_mode_unavailable",
                    [capability.UnavailableReasonCode ?? "The requested webhook provider mode is unavailable."]);
            }

            var targetValidationFailure = await ValidateTargetConfigurationAsync(
                consumer,
                targetMode,
                capability,
                transactionCancellationToken);
            if (targetValidationFailure is not null)
            {
                return Failure(targetValidationFailure.Value.Code, [targetValidationFailure.Value.Message]);
            }

            var uncertainPublicationCount = await providerPublicationRepository.CountUncertainByConsumerAsync(
                request.TenantId,
                consumer.Id,
                transactionCancellationToken);
            if (uncertainPublicationCount > 0 && !request.AcknowledgeUncertainProviderPublications)
            {
                return Failure(
                    "webhook_consumer_provider_mode_uncertain_publications",
                    [$"Acknowledge {uncertainPublicationCount} unknown or manual-reconciliation provider publication(s) before changing provider mode."]);
            }

            var previousMode = consumer.ProviderMode;
            var previousVersion = consumer.ConfigurationVersion;
            var now = timeProvider.GetUtcNow().UtcDateTime;
            consumer.ChangeProviderMode(targetMode, now);
            consumer.UpdatedBy = actorUserId;

            var persisted = await consumerRepository.UpdateAsync(consumer, transactionCancellationToken);
            await auditLogRepository.Create(new AuditLog
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                Tenant = null!,
                EntityType = nameof(WebhookConsumer),
                EntityId = consumer.Id.ToString("D"),
                Action = "WebhookConsumerProviderModeChanged",
                OldValues = JsonSerializer.Serialize(new
                {
                    configurationVersion = previousVersion,
                    providerMode = NormalizedLookupMetadata.WebhookProviderMode((int)previousMode).Code
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    configurationVersion = consumer.ConfigurationVersion,
                    providerMode = NormalizedLookupMetadata.WebhookProviderMode((int)targetMode).Code,
                    pendingWorkDecision = NormalizedLookupMetadata
                        .WebhookPendingWorkDecision(request.PendingWorkDecisionId).Code,
                    migratedWorkCount = 0,
                    uncertainProviderPublicationCount = uncertainPublicationCount,
                    uncertainProviderPublicationsAcknowledged = request.AcknowledgeUncertainProviderPublications,
                    reason = request.PendingWorkReason.Trim(),
                    actorReference,
                    outcome = "applied"
                }),
                AffectedColumns = JsonSerializer.Serialize(new[]
                {
                    nameof(WebhookConsumer.ProviderModeId),
                    nameof(WebhookConsumer.ConfigurationVersion)
                }),
                ActorId = actorUserId,
                Timestamp = now
            });

            var warning = uncertainPublicationCount > 0
                ? $" {uncertainPublicationCount} uncertain provider publication(s) remain on their original snapshots."
                : string.Empty;
            return new BaseCommandResponse<Guid>
            {
                Id = persisted.Id,
                Success = true,
                Message = $"Webhook consumer provider mode changed; existing pending work preserved.{warning}"
            };
        }, cancellationToken);
    }

    private async Task<(string Code, string Message)?> ValidateTargetConfigurationAsync(
        WebhookConsumer consumer,
        WebhookProviderMode targetMode,
        WebhookProviderModeCapabilityResolution capability,
        CancellationToken cancellationToken)
    {
        if (targetMode is WebhookProviderMode.Local or WebhookProviderMode.Composite &&
            !await endpointRepository.HasActiveSubscribedEndpointByConsumerAsync(
                consumer.TenantId,
                consumer.Id,
                cancellationToken))
        {
            return (
                "webhook_consumer_provider_mode_local_target_required",
                "Local and Composite modes require at least one active endpoint with an enabled subscription.");
        }

        if (targetMode is not (WebhookProviderMode.Svix or WebhookProviderMode.Composite))
        {
            return null;
        }

        var binding = consumer.GetVerifiedProviderBinding(WebhookProviderKind.Svix);
        if (binding is null ||
            !string.Equals(binding.ProviderEnvironment, capability.ProviderEnvironment, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(binding.ProviderVersion, capability.ProviderVersion, StringComparison.Ordinal) ||
            !string.Equals(binding.CapabilityResolutionVersion, capability.ResolutionVersion, StringComparison.Ordinal))
        {
            return (
                "webhook_consumer_provider_mode_binding_required",
                "Svix and Composite modes require exactly one verified Svix binding matching the active provider profile.");
        }

        return null;
    }

    private static List<string> Validate(UpdateWebhookConsumerProviderModeCommand request)
    {
        var errors = new List<string>();
        if (request.TenantId == Guid.Empty)
        {
            errors.Add("Tenant id is required.");
        }

        if (request.ConsumerId == Guid.Empty)
        {
            errors.Add("Consumer id is required.");
        }

        if (!Enum.IsDefined(typeof(WebhookProviderMode), request.ProviderModeId))
        {
            errors.Add("Provider mode is invalid.");
        }

        if (request.ExpectedConfigurationVersion < 1)
        {
            errors.Add("Expected configuration version must be positive.");
        }

        if (!Enum.IsDefined(typeof(WebhookPendingWorkDecision), request.PendingWorkDecisionId))
        {
            errors.Add("Pending work decision is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.PendingWorkReason) || request.PendingWorkReason.Trim().Length > 500)
        {
            errors.Add("Pending work reason is required and must be 500 characters or fewer.");
        }

        return errors;
    }

    private bool TryResolveActor(out string actorReference, out Guid? actorUserId)
    {
        if (currentUserService.UserId is { } userId)
        {
            actorReference = $"user:{userId:D}";
            actorUserId = userId;
            return true;
        }

        if (machinePrincipalAccessor.Current is { } machine)
        {
            actorReference = $"machine:{machine.OwnerType}:{machine.OwnerId:D}";
            actorUserId = null;
            return true;
        }

        actorReference = string.Empty;
        actorUserId = null;
        return false;
    }

    private static BaseCommandResponse<Guid> Failure(string code, IReadOnlyList<string> errors) =>
        new()
        {
            Success = false,
            Message = errors[0],
            FailureCode = code,
            Errors = errors.ToList()
        };
}
