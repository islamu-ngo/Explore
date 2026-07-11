// ABOUTME: Registry contract for canonical webhook event type discovery and validation.
// ABOUTME: Keeps provider adapters and APIs from hard-coding event catalog lists.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookEventTypeRegistry
{
    IReadOnlyCollection<WebhookEventTypeDescriptor> GetAll();

    WebhookEventTypeDescriptor? FindByName(string name);

    bool IsKnownEventType(string name);
}

