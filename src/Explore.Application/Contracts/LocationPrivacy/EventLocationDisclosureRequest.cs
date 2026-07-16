// ABOUTME: Immutable input contract for one purpose-scoped EventLocation disclosure decision.
// ABOUTME: Carries association and requester identity without embedding registration-access state.

namespace Explore.Application.Contracts.LocationPrivacy;

public sealed record EventLocationDisclosureRequest(
    Guid TenantId,
    Guid EventId,
    Guid EventLocationId,
    Guid? RoomId,
    Guid? RequesterUserId,
    EventLocationDisclosurePurpose Purpose);
