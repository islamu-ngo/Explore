// ABOUTME: Handles endpoint test-webhook scheduling through canonical message and LocalProvider attempt rows.
// ABOUTME: Keeps endpoint tests durable and worker-driven instead of sending HTTP inside the API request.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class TestWebhookEndpointCommandHandler(
    IWebhookEndpointRepository endpointRepository,
    IWebhookMessageRepository messageRepository,
    IWebhookDeliveryAttemptRepository attemptRepository,
    IWebhookPayloadBuilder payloadBuilder)
    : IRequestHandler<TestWebhookEndpointCommand, BaseCommandResponse<Guid>>
{
    private const int PayloadRetentionDays = 1;

    public async Task<BaseCommandResponse<Guid>> Handle(
        TestWebhookEndpointCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return Failure("webhook_endpoint_test_validation_failed", validationErrors);
        }

        var endpoint = await endpointRepository.GetByTenantAndIdAsync(
            request.TenantId,
            request.EndpointId,
            cancellationToken);
        if (endpoint is null || endpoint.Status == WebhookEndpointStatus.Archived)
        {
            return Failure("webhook_endpoint_not_found", ["Webhook endpoint was not found."]);
        }

        if (endpoint.Status != WebhookEndpointStatus.Active)
        {
            return Failure("webhook_endpoint_not_active", ["Webhook endpoint must be active before a test delivery can be scheduled."]);
        }

        if (endpoint.Consumer is null || endpoint.Consumer.Status != WebhookConsumerStatus.Active)
        {
            return Failure("webhook_consumer_not_found", ["Webhook consumer was not found."]);
        }

        if (!CanScheduleLocalTest(endpoint.Consumer.ProviderMode))
        {
            return Failure(
                "webhook_endpoint_test_provider_managed",
                ["Endpoint tests are only scheduled locally for Local or Composite webhook consumers. Use the provider portal for Svix-managed endpoints."]);
        }

        var now = DateTimeOffset.UtcNow;
        var messageId = Guid.CreateVersion7();
        var payload = await payloadBuilder.BuildAsync(
            CreateBuildContext(request.TenantId, endpoint, messageId, now),
            cancellationToken);
        if (!payload.Succeeded)
        {
            return Failure(
                payload.FailureCategory ?? "webhook_endpoint_test_payload_failed",
                [payload.SafeDetail ?? "Webhook endpoint test payload could not be built."]);
        }

        var created = await messageRepository.CreateAsync(
            new WebhookMessage
            {
                Id = messageId,
                TenantId = request.TenantId,
                EventType = WebhookEventNames.WebhookTest,
                EventId = messageId.ToString("D"),
                AggregateKind = "WebhookEndpoint",
                AggregateId = endpoint.Id,
                ConsumerId = endpoint.ConsumerId,
                PayloadJson = payload.RawPayloadJson,
                PayloadHash = payload.PayloadHash!,
                PayloadRetentionUntil = payload.PayloadRetentionUntil!.Value.UtcDateTime,
                CreatedAt = now.UtcDateTime
            },
            cancellationToken);

        await attemptRepository.CreateAsync(
            new WebhookDeliveryAttempt
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                MessageId = created.Id,
                EndpointId = endpoint.Id,
                AttemptNumber = 1,
                Status = WebhookDeliveryAttemptStatus.Scheduled,
                ScheduledAt = now.UtcDateTime,
                CreatedAt = now.UtcDateTime
            },
            cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Id = created.Id,
            Success = true,
            Message = "Webhook endpoint test scheduled."
        };
    }

    private static WebhookEventBuildContext CreateBuildContext(
        Guid tenantId,
        WebhookEndpoint endpoint,
        Guid messageId,
        DateTimeOffset now) =>
        new(
            messageId,
            tenantId,
            WebhookEventNames.WebhookTest,
            messageId.ToString("D"),
            "WebhookEndpoint",
            endpoint.Id,
            now,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["endpointId"] = endpoint.Id.ToString("D"),
                ["consumerId"] = endpoint.ConsumerId.ToString("D"),
                ["providerMode"] = endpoint.Consumer!.ProviderMode.ToString(),
                ["requestedAt"] = now.ToString("O")
            },
            endpoint.ConsumerId,
            PayloadRetentionDays);

    private static bool CanScheduleLocalTest(WebhookProviderMode providerMode) =>
        providerMode is WebhookProviderMode.Local or WebhookProviderMode.Composite;

    private static List<string> Validate(TestWebhookEndpointCommand request)
    {
        var errors = new List<string>();
        if (request.TenantId == Guid.Empty)
        {
            errors.Add("Tenant id is required.");
        }

        if (request.EndpointId == Guid.Empty)
        {
            errors.Add("Endpoint id is required.");
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
