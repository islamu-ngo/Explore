// ABOUTME: Minimal local-state CQRS commands for actor-bound organizer payment connections.
// ABOUTME: Carries explicit organizer actor identity so session or admin status never selects a recipient.

using Explore.Application.Responses;
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

public sealed record CreateOrganizerPaymentOnboardingLinkCommand(
    Guid TenantId,
    Guid OrganizerActorId,
    string ProviderCode,
    string ConnectPlatformId,
    Uri ReturnUrl,
    Uri RefreshUrl) : IRequest<BaseCommandResponse<OrganizerPaymentOnboardingLinkResult>>;

public sealed record OrganizerPaymentOnboardingLinkResult(
    Guid ConnectionId,
    string ExternalAccountId,
    Uri OnboardingUrl,
    bool ReusedExistingConnection);
