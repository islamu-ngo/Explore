// ABOUTME: Secured operator commands that control the instance scheduler and its individual jobs.
// ABOUTME: Authorizes every action through instance-setting update metadata before any handler runs.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Scheduling.Requests.Commands;

/// <summary>
/// Shared identity and authorization metadata for scheduler control actions. Every action is an instance-setting
/// update rather than a bespoke permission, so scheduler control follows the same authority as other operator
/// surfaces instead of introducing a parallel policy that could drift from it.
/// </summary>
public abstract class SchedulerAdminCommandBase : IRequest<BaseCommandResponse<string>>, ISecureRequest
{
    public const string SettingKey = "scheduler.admin";

    string? ISecureRequest.ResourceId => SettingKey;

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["settingKey"] = SettingKey
    };
}

/// <summary>Identifies one job by its scheduler group and name, both taken from the request route.</summary>
public abstract class SchedulerAdminJobCommandBase : SchedulerAdminCommandBase
{
    public string Group { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Moves the whole scheduler to standby. This stops <em>all</em> background work — email dispatch, retention
/// sweeps, storage reconciliation — from a single action, so it requires the operator to type the scheduler name
/// back, matching how the platform guards other instance-wide destructive intent. Resume and per-job actions are
/// narrow and carry no such guard.
/// </summary>
[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class PauseSchedulerCommand : SchedulerAdminCommandBase
{
    public string? ConfirmationText { get; init; }
}

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class ResumeSchedulerCommand : SchedulerAdminCommandBase;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class PauseSchedulerJobCommand : SchedulerAdminJobCommandBase;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class ResumeSchedulerJobCommand : SchedulerAdminJobCommandBase;

[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class TriggerSchedulerJobCommand : SchedulerAdminJobCommandBase;

/// <summary>Clears the scheduler's error state on a job's triggers so they resume normal firing.</summary>
[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class ResetSchedulerJobErrorStateCommand : SchedulerAdminJobCommandBase;

/// <summary>Requests cooperative cancellation of a job's currently executing instances.</summary>
[AuthorizeResource(ResourceKinds.InstanceSetting, AuthorizationActions.InstanceSettings.Update)]
public sealed class InterruptSchedulerJobCommand : SchedulerAdminJobCommandBase;
