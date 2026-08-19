// ABOUTME: Query request for the current tenant's effective moderation reporting routing state.
// ABOUTME: Carries tenant-setting authorization context so provider routing reads fail closed.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventReporting;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Queries;

[AuthorizeResource(ResourceKinds.TenantSetting, AuthorizationActions.TenantSettings.View)]
public sealed record GetReportingRoutingStateRequest(Guid TenantId) : IRequest<ReportingRoutingStateDto>, ISecureRequest
{
    private const string SettingKey = "moderation-reporting";

    public string? ResourceId => $"{TenantId}:{SettingKey}";

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new TenantSettingAuthorizationFacts(TenantId);
}
