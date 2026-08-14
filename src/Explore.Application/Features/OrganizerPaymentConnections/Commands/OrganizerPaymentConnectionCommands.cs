// ABOUTME: Minimal local-state CQRS commands for actor-bound organizer payment connections.
// ABOUTME: Carries explicit organizer actor identity so session or admin status never selects a recipient.

using Explore.Application.Responses;
using Explore.Application.Authorization;
using MediatR;

namespace Explore.Application.Features.OrganizerPaymentConnections.Commands;

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

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManagePaidEventCommerce)]
public sealed record CreateOrganizerPaymentOnboardingLinkCommand(
    Guid EventId,
    Uri ReturnUrl,
    Uri RefreshUrl) : IRequest<BaseCommandResponse<OrganizerPaymentOnboardingLinkResult>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object> { ["eventId"] = EventId.ToString() };
}

public sealed record OrganizerPaymentOnboardingLinkResult(
    Uri OnboardingUrl,
    bool ReusedExistingConnection);
