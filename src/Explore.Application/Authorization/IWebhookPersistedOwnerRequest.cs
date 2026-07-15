// ABOUTME: Marks webhook requests whose authoritative owner must be loaded from a persisted resource.
// ABOUTME: Prevents existing-resource authorization from trusting caller-provided tenant or owner attributes.

namespace Explore.Application.Authorization;

public interface IWebhookPersistedOwnerRequest
{
    WebhookOwnedResourceKind OwnedResourceKind { get; }
    Guid OwnedResourceId { get; }
}

public enum WebhookOwnedResourceKind
{
    Consumer = 1,
    Endpoint = 2,
    Message = 3,
    DeliveryAttempt = 4
}
