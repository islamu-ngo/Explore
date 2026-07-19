// ABOUTME: Application contract for recording PII-free exact EventLocation read security evidence.
// ABOUTME: Carries only stable identities, a closed purpose, the decision, and optional trace identifiers.

using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Services;

public sealed record EventLocationExactReadAuditRequest(
    Guid TenantId,
    Guid EventLocationId,
    Guid RequesterUserId,
    EventLocationExactReadPurposeEnum Purpose,
    bool WasAuthorized,
    Guid? CorrelationId = null,
    Guid? TraceId = null);

public interface IEventLocationExactReadAuditService
{
    Task RecordManyAsync(
        IReadOnlyCollection<EventLocationExactReadAuditRequest> requests,
        CancellationToken cancellationToken);
}
