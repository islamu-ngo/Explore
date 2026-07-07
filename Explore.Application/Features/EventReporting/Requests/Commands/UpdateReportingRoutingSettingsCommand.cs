// ABOUTME: Command for updating current-tenant moderation reporting provider routing settings.
// ABOUTME: Carries tenant-setting authorization metadata so locked routing updates fail closed.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Commands;

[AuthorizeResource(ResourceKinds.TenantSetting, AuthorizationActions.TenantSettings.Update)]
public sealed record UpdateReportingRoutingSettingsCommand(Guid TenantId, Guid UserId, UpdateReportingRoutingSettingsDto Settings)
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    private const string SettingKey = "moderation-reporting";

    public string? ResourceId => $"{TenantId}:{SettingKey}";

    public IDictionary<string, object>? ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["settingKey"] = SettingKey
    };
}
