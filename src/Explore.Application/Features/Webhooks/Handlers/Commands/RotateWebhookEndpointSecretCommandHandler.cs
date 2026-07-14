// ABOUTME: Handles webhook endpoint signing secret reference rotation with bounded previous-secret overlap.
// ABOUTME: Keeps secret material external to the database and persists only secret references plus version metadata.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class RotateWebhookEndpointSecretCommandHandler(
    IWebhookEndpointRepository endpointRepository,
    IWebhookConsumerRepository consumerRepository,
    IWebhookProviderCapabilityResolver capabilityResolver)
    : IRequestHandler<RotateWebhookEndpointSecretCommand, BaseCommandResponse<Guid>>
{
    private const int DefaultPreviousSecretValidForSeconds = 86_400;
    private const int MaxPreviousSecretValidForSeconds = 2_592_000;

    public async Task<BaseCommandResponse<Guid>> Handle(
        RotateWebhookEndpointSecretCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request, out var normalizedSecretRef, out var previousSecretValidForSeconds);
        if (validationErrors.Count > 0)
        {
            return Failure("webhook_endpoint_secret_validation_failed", validationErrors);
        }

        var endpoint = await endpointRepository.GetByTenantAndIdForUpdateAsync(
            request.TenantId,
            request.EndpointId,
            cancellationToken);
        if (endpoint is null || endpoint.Status == WebhookEndpointStatus.Archived)
        {
            return Failure("webhook_endpoint_not_found", ["Webhook endpoint was not found."]);
        }

        var consumer = await consumerRepository.GetByTenantAndIdAsync(
            request.TenantId,
            endpoint.ConsumerId,
            cancellationToken);
        if (consumer is null)
        {
            return Failure(
                "webhook_endpoint_management_unavailable",
                ["Webhook consumer was not found."]);
        }

        if (!WebhookEndpointCapabilityPolicy.CanManageLocalEndpoint(
                capabilityResolver,
                consumer.ProviderMode,
                out var capabilityFailure))
        {
            return Failure(
                "webhook_endpoint_management_unavailable",
                [capabilityFailure]);
        }

        if (string.Equals(endpoint.SecretRef, normalizedSecretRef, StringComparison.Ordinal))
        {
            return Failure("webhook_endpoint_secret_unchanged", ["New secret reference must differ from the current secret reference."]);
        }

        var now = DateTime.UtcNow;
        endpoint.PreviousSecretRef = endpoint.SecretRef;
        endpoint.PreviousSecretValidUntil = previousSecretValidForSeconds == 0
            ? now
            : now.AddSeconds(previousSecretValidForSeconds);
        endpoint.SecretRef = normalizedSecretRef;
        endpoint.SecretVersion += 1;
        endpoint.UpdatedAt = now;

        var persisted = await endpointRepository.UpdateAsync(endpoint, cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Id = persisted.Id,
            Success = true,
            Message = "Webhook endpoint secret rotated."
        };
    }

    private static List<string> Validate(
        RotateWebhookEndpointSecretCommand request,
        out string normalizedSecretRef,
        out int previousSecretValidForSeconds)
    {
        var errors = new List<string>();
        normalizedSecretRef = request.NewSecretRef.Trim();
        previousSecretValidForSeconds = request.PreviousSecretValidForSeconds ?? DefaultPreviousSecretValidForSeconds;

        if (request.TenantId == Guid.Empty)
        {
            errors.Add("Tenant id is required.");
        }

        if (request.EndpointId == Guid.Empty)
        {
            errors.Add("Endpoint id is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedSecretRef) || normalizedSecretRef.Length > 500)
        {
            errors.Add("New secret reference is required and must be 500 characters or fewer.");
        }

        if (request.PreviousSecretValidForSeconds is < 0 or > MaxPreviousSecretValidForSeconds)
        {
            errors.Add("Previous secret validity must be between 0 and 2592000 seconds.");
        }

        return errors;
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
