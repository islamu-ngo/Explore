// ABOUTME: MediatR command for instance admins to update moderation reporting provider lock flags.
// ABOUTME: Uses the instance-setting authorization resource so lock changes stay server-authorized.

namespace Explore.Application.Features.EventReporting.Requests.Commands;

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Responses;
using MediatR;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed record UpdateReportingProviderLocksCommand(
    Guid UserId,
    UpdateReportingProviderLocksDto Locks)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    private const string SettingKey = "moderation-reporting-locks";

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey,
    };
}
