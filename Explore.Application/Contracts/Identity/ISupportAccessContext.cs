// ABOUTME: Application contract for the active support-access context on a request.
// ABOUTME: Carries support metadata without replacing the real current user identity.

using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Identity;

public interface ISupportAccessContext
{
    bool IsActive { get; }
    Guid? SessionId { get; }
    Guid? ActorUserId { get; }
    Guid? TargetTenantId { get; }
    Guid? TargetTenantUserId { get; }
    SupportAccessModeEnum? Mode { get; }
    DateTimeOffset? StartedAtUtc { get; }
    DateTimeOffset? ExpiresAtUtc { get; }
    string? ReasonCode { get; }
    string? TicketReference { get; }
    bool AllowsWrites { get; }
    bool WasForwarded { get; }
}
