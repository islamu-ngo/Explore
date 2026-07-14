// ABOUTME: Explicit application failure for reuse of a webhook semantic identity with changed immutable data.
// ABOUTME: Prevents redelivery or concurrency recovery from hiding payload and routing conflicts.

namespace Explore.Application.Exceptions;

public sealed class WebhookMaterializationConflictException : InvalidOperationException
{
    public WebhookMaterializationConflictException(string message)
        : base(message)
    {
    }
}
