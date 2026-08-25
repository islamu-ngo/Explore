// ABOUTME: Instance-admin command for pausing or resuming every SMTP dispatch admission path.
// ABOUTME: Persists a bounded operator reason and audit actor in the singleton processor state.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EmailDispatch.Requests.Commands;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed record SetEmailDispatchProcessorPauseStateCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public bool IsPaused { get; init; }
    public string? PauseReason { get; init; }
    public Guid? ChangedBy { get; init; }

    string ISecureRequest.ResourceId => EmailDispatchProcessorControl.SettingKey;

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        InstanceScopedAuthorizationFacts.Instance;
}
