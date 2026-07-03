// ABOUTME: Handles endpoint update validation, URL uniqueness, and subscription replacement.
// ABOUTME: Keeps secret rotation separate from normal endpoint edits and delegates persistence to repositories.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class UpdateWebhookEndpointCommandHandler(
    IWebhookEndpointRepository endpointRepository,
    IWebhookEventTypeRepository eventTypeRepository)
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

        var endpoint = await endpointRepository.GetByTenantAndIdForUpdateAsync(
            request.TenantId,
            request.EndpointId,
            cancellationToken);
        if (endpoint is null || endpoint.Status == WebhookEndpointStatus.Archived)
        {
            return Failure("webhook_endpoint_not_found", ["Webhook endpoint was not found."]);
        }

        var existingEndpoint = await endpointRepository.GetByTenantConsumerAndUrlAsync(
            request.TenantId,
            endpoint.ConsumerId,
            normalizedUrl,
            cancellationToken);
        if (existingEndpoint is not null && existingEndpoint.Id != endpoint.Id)
        {
            return Failure("webhook_endpoint_url_conflict", ["Webhook endpoint URL is already configured for this consumer."]);
        }

        var eventTypes = await eventTypeRepository.GetByIdsAsync(eventTypeIds, cancellationToken);
        if (eventTypes.Count != eventTypeIds.Length || eventTypes.Any(eventType => !eventType.IsEnabled))
        {
            return Failure("webhook_endpoint_event_types_invalid", ["One or more webhook event types are invalid or disabled."]);
        }

        var now = DateTime.UtcNow;
        endpoint.Url = normalizedUrl;
        endpoint.Description = NormalizeOptional(request.Description);
        endpoint.MaxAttempts = request.MaxAttempts ?? endpoint.MaxAttempts;
        endpoint.TimeoutSeconds = request.TimeoutSeconds ?? endpoint.TimeoutSeconds;
        endpoint.RateLimitPerMinute = request.RateLimitPerMinute;
        endpoint.UpdatedAt = now;

        var subscriptions = eventTypes
            .Select(eventType => new WebhookEndpointSubscription
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                EndpointId = endpoint.Id,
                EventTypeId = eventType.Id,
                IsEnabled = true,
                CreatedAt = now
            })
            .ToArray();

        var persisted = await endpointRepository.UpdateWithSubscriptionsAsync(
            endpoint,
            subscriptions,
            cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Id = persisted.Id,
            Success = true,
            Message = "Webhook endpoint updated."
        };
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

        if (request.TenantId == Guid.Empty)
        {
            errors.Add("Tenant id is required.");
        }

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

    private static BaseCommandResponse<Guid> Failure(string code, IReadOnlyList<string> errors) =>
        new()
        {
            Success = false,
            Message = errors[0],
            FailureCode = code,
            Errors = errors.ToList()
        };
}
