// ABOUTME: Pointer-only scheduler payload for delayed EmailDispatchOutbox work.
// ABOUTME: Carries durable identifiers only; message content and transport data remain in PostgreSQL.

namespace Explore.Application.Contracts.Infrastructure;

public sealed record ScheduledEmailDispatchPointer(
    Guid TenantId,
    Guid PublishEventId,
    string UseCase,
    Guid? EventId,
    Guid? RegistrationIntentId);
