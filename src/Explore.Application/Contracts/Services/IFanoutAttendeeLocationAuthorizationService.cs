// ABOUTME: Trusted worker seam for recipient-specific attendee location authorization.
// ABOUTME: Returns only current disclosure state and allowed fields, never mutable location values.

using System.Collections.Immutable;
using Explore.Application.Contracts.LocationPrivacy;

namespace Explore.Application.Contracts.Services;

public interface IFanoutAttendeeLocationAuthorizationService
{
    Task<FanoutAttendeeLocationAuthorizationResult> AuthorizeAsync(
        FanoutAttendeeLocationAuthorizationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record FanoutAttendeeLocationAuthorizationRequest(
    Guid TenantId,
    Guid EventId,
    Guid RecipientUserId,
    Guid EventLocationId,
    Guid? RoomId);

public sealed record FanoutAttendeeLocationAuthorizationResult(
    Guid TenantId,
    Guid EventId,
    Guid RecipientUserId,
    Guid EventLocationId,
    Guid? RoomId,
    EventLocationDisclosureState State,
    ImmutableArray<EventLocationDisclosureField> AllowedFields);
