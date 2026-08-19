// ABOUTME: Instance-admin command for pausing or resuming every SMTP dispatch admission path.
// ABOUTME: Persists a bounded operator reason and audit actor in the singleton processor state.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class SetEmailDispatchProcessorPauseStateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }
    public Guid? ChangedBy { get; set; }

    string ISecureRequest.ResourceId => EmailDispatchProcessorControl.SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
