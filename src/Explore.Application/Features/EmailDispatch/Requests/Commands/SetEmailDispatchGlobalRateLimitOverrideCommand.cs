// ABOUTME: Instance-admin command for setting or clearing the persisted global SMTP rate override.
// ABOUTME: Leaves the configured processor rate authoritative whenever the nullable override is cleared.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class SetEmailDispatchGlobalRateLimitOverrideCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public int? RateLimitPerMinute { get; set; }
    public Guid? ChangedBy { get; set; }

    string ISecureRequest.ResourceId => EmailDispatchProcessorControl.SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
