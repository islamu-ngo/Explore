// ABOUTME: Marks webhook requests whose authorization target is a caller-selected owner scope.
// ABOUTME: Lets the authorization pipeline replace transport ids with a persistence-backed canonical scope.

namespace Explore.Application.Authorization;

public interface IWebhookOwnerScopedRequest
{
    int OwnerKindId { get; }
    Guid? OwnerId { get; }
}
