// ABOUTME: Command contract for tenant moderation reporting provider configuration tests.
// ABOUTME: Authorizes provider test actions as tenant-setting updates without exposing secrets.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Commands;

[AuthorizeResource(ResourceKinds.TenantSetting, AuthorizationActions.TenantSettings.Update)]
public sealed record TestReportingProviderTargetCommand(
    Guid TenantId,
    Guid UserId,
    EventReportExternalProvider Provider) : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    private const string SettingKey = "moderation-reporting";

    string? ISecureRequest.ResourceId => $"{TenantId}:{SettingKey}";

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString(),
        ["settingKey"] = SettingKey,
        ["provider"] = Provider.ToString()
    };
}
