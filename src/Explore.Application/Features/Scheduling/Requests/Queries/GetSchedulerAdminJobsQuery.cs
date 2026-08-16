// ABOUTME: Secured query listing every scheduled job with its trigger states for the operator surface.
// ABOUTME: Shares the scheduler administration setting key so visibility matches the overview resource exactly.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Scheduling;
using MediatR;

namespace Explore.Application.Features.Scheduling.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetSchedulerAdminJobsQuery : IRequest<IReadOnlyList<SchedulerAdminJobDto>>, ISecureRequest
{
    public const string SettingKey = GetSchedulerAdminOverviewQuery.SettingKey;

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey
    };
}
