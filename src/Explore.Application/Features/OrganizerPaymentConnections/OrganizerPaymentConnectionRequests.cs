// ABOUTME: Minimal local-state CQRS requests for actor-bound organizer payment connections.
// ABOUTME: Carries explicit organizer actor identity so session or admin status never selects a recipient.

using Explore.Application.DTOs.OrganizerPaymentConnections;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.OrganizerPaymentConnections;

public sealed record RecordOrganizerPaymentConnectionCommand(
    Guid TenantId,
    Guid OrganizerActorId,
    string ProviderCode,
    string ConnectPlatformId,
    string ExternalAccountId) : IRequest<BaseCommandResponse<Guid>>;

public sealed record ReplaceOrganizerPaymentConnectionCommand(
    Guid TenantId,
    Guid OrganizerActorId,
    Guid CurrentConnectionId,
    string NewExternalAccountId) : IRequest<BaseCommandResponse<Guid>>;

public sealed record DisableOrganizerPaymentConnectionCommand(
    Guid TenantId,
    Guid OrganizerActorId,
    Guid ConnectionId,
    string ReasonCode) : IRequest<BaseCommandResponse<Guid>>;

public sealed record GetOrganizerPaymentConnectionQuery(
    Guid TenantId,
    Guid OrganizerActorId,
    Guid ConnectionId) : IRequest<OrganizerPaymentConnectionDto?>;

public sealed record ListOrganizerPaymentConnectionsQuery(Guid TenantId, Guid OrganizerActorId)
    : IRequest<IReadOnlyList<OrganizerPaymentConnectionDto>>;
