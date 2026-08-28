// ABOUTME: Authorized query for the current tenant's effective event-reporting intake policy.
// ABOUTME: Uses the canonical setting key so authorization providers can resolve fresh instance-lock metadata.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventReporting;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Queries;

[AuthorizeResource(ResourceKinds.TenantSetting, AuthorizationActions.TenantSettings.View)]
public sealed record GetTenantReportingIntakePolicyQuery(Guid TenantId)
    : IRequest<TenantReportingIntakePolicyDto>, ISecureRequest
{
    string? ISecureRequest.ResourceId => GovernanceSettingKeys.EventReporting.IntakeEnabled;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts => new TenantSettingAuthorizationFacts(
        TenantId,
        GovernanceSettingKeys.EventReporting.IntakeEnabled);
}
