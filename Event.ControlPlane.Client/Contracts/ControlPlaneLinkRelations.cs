// ABOUTME: Defines stable HAL link relation names used by shared control-plane UI.
// ABOUTME: Avoids duplicated action strings across embedded and separate Blazor hosts.

namespace Event.ControlPlane.Client.Contracts;

public static class ControlPlaneLinkRelations
{
    public const string Self = "self";
    public const string Create = "create";
    public const string Edit = "edit";
    public const string Delete = "delete";
    public const string Provision = "provision";
    public const string Suspend = "suspend";
    public const string Archive = "archive";
    public const string Purge = "purge";
    public const string Verify = "verify";
    public const string Retry = "retry";
    public const string Test = "test";
    public const string DeploymentModeRunbook = "deployment-mode-runbook";
    public const string TransitionToMultiTenant = "transition-to-multi-tenant";
    public const string TransitionToSingleTenant = "transition-to-single-tenant";
}
