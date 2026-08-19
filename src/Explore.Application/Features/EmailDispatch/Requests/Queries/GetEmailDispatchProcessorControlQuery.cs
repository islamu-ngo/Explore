// ABOUTME: Secured query for sanitized instance-wide SMTP processor control state.
// ABOUTME: Uses the instance-setting resource so tenant administrators cannot inspect global controls.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EmailDispatch;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Queries;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.View)]
public sealed class GetEmailDispatchProcessorControlQuery : IRequest<EmailDispatchProcessorControlDto>, ISecureRequest
{
    string ISecureRequest.ResourceId => EmailDispatchProcessorControl.SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
