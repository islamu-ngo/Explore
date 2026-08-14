// ABOUTME: Minimal local-state CQRS requests for actor-bound organizer payment connections.
// ABOUTME: Carries explicit organizer actor identity so session or admin status never selects a recipient.

using Explore.Application.DTOs.OrganizerPaymentConnections;
using MediatR;

namespace Explore.Application.Features.OrganizerPaymentConnections;

public sealed record GetOrganizerPaymentConnectionQuery(
    Guid TenantId,
    Guid OrganizerActorId,
    Guid ConnectionId) : IRequest<OrganizerPaymentConnectionDto?>;

public sealed record ListOrganizerPaymentConnectionsQuery(Guid TenantId, Guid OrganizerActorId)
    : IRequest<IReadOnlyList<OrganizerPaymentConnectionDto>>;
