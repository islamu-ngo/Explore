// ABOUTME: Pointer-only RabbitMQ payload contract for durable EmailDispatchOutbox records.
// ABOUTME: Excludes recipient, subject, body, provider secrets, and other email content by design.

using Explore.Domain;

namespace Explore.Application.Contracts.Infrastructure;

public sealed record EmailDispatchPointer(
    Guid TenantId,
    Guid PublishEventId,
    EmailDispatchKind Kind,
    string SourceType,
    Guid? SourceId,
    Guid? EventId,
    Guid? RegistrationIntentId)
{
    public static EmailDispatchPointer FromOutbox(EmailDispatchOutbox dispatch)
    {
        ArgumentNullException.ThrowIfNull(dispatch);

        return new EmailDispatchPointer(
            dispatch.TenantId,
            dispatch.PublishEventId,
            dispatch.Kind,
            dispatch.SourceType,
            dispatch.SourceId,
            dispatch.EventId,
            dispatch.RegistrationIntentId);
    }
}
