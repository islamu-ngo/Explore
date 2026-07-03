// ABOUTME: Generates JSON schema and example payloads for canonical webhook event types.
// ABOUTME: Keeps event catalog documentation and API surfaces aligned with payload builders.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookEventSchemaProvider
{
    string CreateSchemaJson(WebhookEventTypeDescriptor descriptor);

    string CreateExamplePayloadJson(WebhookEventTypeDescriptor descriptor);
}

