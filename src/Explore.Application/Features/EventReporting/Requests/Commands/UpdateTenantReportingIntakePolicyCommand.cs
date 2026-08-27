// ABOUTME: Authorized command for changing the current tenant's event-reporting intake policy.
// ABOUTME: Carries only server-derived identity plus the requested enabled state into the guarded mutation boundary.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Commands;

[AuthorizeResource(ResourceKinds.TenantSetting, AuthorizationActions.TenantSettings.Update)]
public sealed record UpdateTenantReportingIntakePolicyCommand(
    Guid TenantId,
    Guid ActorUserId,
    UpdateTenantReportingIntakePolicyDto Policy)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => GovernanceSettingKeys.EventReporting.IntakeEnabled;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts => new TenantSettingAuthorizationFacts(
        TenantId,
        GovernanceSettingKeys.EventReporting.IntakeEnabled);
}
