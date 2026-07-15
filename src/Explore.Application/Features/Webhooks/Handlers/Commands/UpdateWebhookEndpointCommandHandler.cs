// ABOUTME: Handles endpoint update validation, URL uniqueness, and subscription replacement.
// ABOUTME: Keeps secret rotation separate from normal endpoint edits and delegates persistence to repositories.

using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Lookups;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class UpdateWebhookEndpointCommandHandler(
    IWebhookEndpointRepository endpointRepository,
    IWebhookConsumerRepository consumerRepository,
    IWebhookEventTypeRepository eventTypeRepository,
    IWebhookProviderPublicationRepository providerPublicationRepository,
    IWebhookProviderCapabilityResolver capabilityResolver,
    IWebhookAuditEventWriter auditWriter,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IMachinePrincipalAccessor machinePrincipalAccessor,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateWebhookEndpointCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateWebhookEndpointCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request, out var normalizedUrl, out var eventTypeIds);
        if (validationErrors.Count > 0)
        {
            return Failure("webhook_endpoint_validation_failed", validationErrors);
        }

        if (!TryResolveActor(out _, out _))
        {
            return Failure(
                "webhook_endpoint_configuration_actor_required",
                ["An authenticated operator identity is required."]);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var endpoint = await endpointRepository.GetByIdForOwnerOperationAsync(
                request.EndpointId,
                forUpdate: true,
                transactionCancellationToken);
            if (endpoint is null || endpoint.Status == WebhookEndpointStatus.Archived)
            {
                return Failure("webhook_endpoint_not_found", ["Webhook endpoint was not found."]);
            }

            if (endpoint.ConfigurationVersion != request.ExpectedConfigurationVersion)
            {
                return Failure(
                    "webhook_endpoint_configuration_conflict",
                    ["Webhook endpoint configuration changed. Reload it before applying another update."]);
            }

            var consumer = endpoint.Consumer ?? await consumerRepository.GetByIdForOwnerOperationAsync(
                endpoint.ConsumerId,
                forUpdate: false,
                transactionCancellationToken);
            if (consumer is null || consumer.Status != WebhookConsumerStatus.Active)
            {
                return Failure("webhook_consumer_not_found", ["Webhook consumer was not found."]);
            }

            if (!WebhookEndpointCapabilityPolicy.CanManageLocalEndpoint(
                    capabilityResolver,
                    consumer.ProviderMode,
                    out var capabilityFailure))
            {
                return Failure("webhook_endpoint_management_unavailable", [capabilityFailure]);
            }

            var existingEndpoint = await endpointRepository.GetByConsumerAndUrlForOwnerOperationAsync(
                endpoint.ConsumerId,
                normalizedUrl,
                transactionCancellationToken);
            if (existingEndpoint is not null && existingEndpoint.Id != endpoint.Id)
            {
                return Failure("webhook_endpoint_url_conflict", ["Webhook endpoint URL is already configured for this consumer."]);
            }

            var eventTypes = await eventTypeRepository.GetByIdsAsync(
                eventTypeIds,
                transactionCancellationToken);
            if (eventTypes.Count != eventTypeIds.Length || eventTypes.Any(eventType => !eventType.IsEnabled))
            {
                return Failure("webhook_endpoint_event_types_invalid", ["One or more webhook event types are invalid or disabled."]);
            }

            var uncertainPublicationCount = await providerPublicationRepository.CountUncertainByConsumerAsync(
                consumer.TenantId,
                consumer.Id,
                transactionCancellationToken);
            if (uncertainPublicationCount > 0 && !request.AcknowledgeUncertainProviderPublications)
            {
                return Failure(
                    "webhook_endpoint_configuration_uncertain_publications",
                    [$"Acknowledge {uncertainPublicationCount} unknown or manual-reconciliation provider publication(s) before changing configuration."]);
            }

            var eligibleTargets = await endpointRepository.GetEligiblePendingTargetsForUpdateAsync(
                endpoint.TenantId,
                endpoint.Id,
                transactionCancellationToken);
            var previousVersion = endpoint.ConfigurationVersion;
            var previousHost = GetDestinationHost(endpoint.Url);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            endpoint.UpdateConfiguration(
                normalizedUrl,
                NormalizeOptional(request.Description),
                request.MaxAttempts ?? endpoint.MaxAttempts,
                request.TimeoutSeconds ?? endpoint.TimeoutSeconds,
                request.RateLimitPerMinute,
                now);

            var decision = (WebhookPendingWorkDecision)request.PendingWorkDecisionId;
            var migratedTargetCount = 0;
            if (decision == WebhookPendingWorkDecision.MigrateEligible)
            {
                foreach (var target in eligibleTargets.Where(target =>
                    target.EndpointConfigurationVersion < endpoint.ConfigurationVersion))
                {
                    target.MigratePendingConfiguration(endpoint, new DateTimeOffset(now));
                    migratedTargetCount++;
                }
            }

            var subscriptions = eventTypes
                .Select(eventType => new WebhookEndpointSubscription
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = endpoint.TenantId,
                    InstanceId = endpoint.InstanceId,
                    EndpointId = endpoint.Id,
                    EventTypeId = eventType.Id,
                    IsEnabled = true,
                    CreatedAt = now
                })
                .ToArray();

            var persisted = await endpointRepository.UpdateWithSubscriptionsAsync(
                endpoint,
                subscriptions,
                transactionCancellationToken);
            var pendingWorkDecisionCode = NormalizedLookupMetadata
                .WebhookPendingWorkDecision(request.PendingWorkDecisionId).Code;
            await auditWriter.AppendAsync(
                new WebhookAuditWriteRequest(
                    endpoint.TenantId,
                    WebhookAuditAction.EndpointUpdated,
                    WebhookAuditTargetKind.Endpoint,
                    endpoint.Id,
                    $"pending_work_{pendingWorkDecisionCode.ToLowerInvariant()}",
                    WebhookAuditOutcome.Succeeded,
                    SafeBeforeJson: JsonSerializer.Serialize(new
                    {
                        configurationVersion = previousVersion,
                        destinationHost = previousHost
                    }),
                    SafeAfterJson: JsonSerializer.Serialize(new
                    {
                        configurationVersion = endpoint.ConfigurationVersion,
                        destinationHost = GetDestinationHost(endpoint.Url),
                        pendingWorkDecision = pendingWorkDecisionCode,
                        eligiblePendingTargetCount = eligibleTargets.Count,
                        migratedTargetCount,
                        uncertainProviderPublicationCount = uncertainPublicationCount,
                        uncertainProviderPublicationsAcknowledged = request.AcknowledgeUncertainProviderPublications,
                        outcome = "applied"
                    }),
                    ConfigurationVersion: $"endpoint-v{endpoint.ConfigurationVersion}",
                    EffectiveScopeKind: consumer.Ownership.AuditScopeKind,
                    EffectiveScopeId: consumer.OwnerId),
                transactionCancellationToken);

            if (migratedTargetCount > 0)
            {
                await auditWriter.AppendAsync(
                    new WebhookAuditWriteRequest(
                        endpoint.TenantId,
                        WebhookAuditAction.PendingWorkMigrated,
                        WebhookAuditTargetKind.Endpoint,
                        endpoint.Id,
                        "eligible_local_targets_migrated",
                        WebhookAuditOutcome.Succeeded,
                        SafeAfterJson: JsonSerializer.Serialize(new
                        {
                            pendingWorkDecision = pendingWorkDecisionCode,
                            migratedTargetCount,
                            endpointConfigurationVersion = endpoint.ConfigurationVersion
                        }),
                        ConfigurationVersion: $"endpoint-v{endpoint.ConfigurationVersion}",
                        EffectiveScopeKind: consumer.Ownership.AuditScopeKind,
                        EffectiveScopeId: consumer.OwnerId),
                    transactionCancellationToken);
            }

            var warning = uncertainPublicationCount > 0
                ? $" {uncertainPublicationCount} uncertain provider publication(s) remain on their original snapshots."
                : string.Empty;
            return new BaseCommandResponse<Guid>
            {
                Id = persisted.Id,
                Success = true,
                Message = $"Webhook endpoint updated; {migratedTargetCount} eligible pending target(s) migrated.{warning}"
            };
        }, cancellationToken);
    }

    private static List<string> Validate(
        UpdateWebhookEndpointCommand request,
        out string normalizedUrl,
        out Guid[] eventTypeIds)
    {
        var errors = new List<string>();
        normalizedUrl = string.Empty;
        eventTypeIds = request.EventTypeIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (request.EndpointId == Guid.Empty)
        {
            errors.Add("Endpoint id is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 1000)
        {
            errors.Add("Description must be 1000 characters or fewer.");
        }

        if (request.EventTypeIds.Count == 0 || eventTypeIds.Length == 0)
        {
            errors.Add("At least one event type is required.");
        }

        if (request.EventTypeIds.Count != eventTypeIds.Length)
        {
            errors.Add("Event type ids must be non-empty and unique.");
        }

        if (request.MaxAttempts is < 1 or > 20)
        {
            errors.Add("Max attempts must be between 1 and 20.");
        }

        if (request.TimeoutSeconds is < 1 or > 60)
        {
            errors.Add("Timeout seconds must be between 1 and 60.");
        }

        if (request.RateLimitPerMinute is < 1 or > 10_000)
        {
            errors.Add("Rate limit per minute must be between 1 and 10000.");
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

        if (string.IsNullOrWhiteSpace(request.Url)
            || request.Url.Trim().Length > 2048
            || !Uri.TryCreate(request.Url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("Endpoint URL must be an absolute HTTP or HTTPS URL.");
        }
        else
        {
            normalizedUrl = uri.ToString();
        }

        return errors;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
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

    private static string GetDestinationHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.IdnHost : "unknown";

    private static BaseCommandResponse<Guid> Failure(string code, IReadOnlyList<string> errors) =>
        new()
        {
            Success = false,
            Message = errors[0],
            FailureCode = code,
            Errors = errors.ToList()
        };
}
