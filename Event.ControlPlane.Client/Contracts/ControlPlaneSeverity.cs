// ABOUTME: Defines presentation-neutral severity constants for control-plane summaries.
// ABOUTME: Lets host components map status values to their own visual design system.

namespace Event.ControlPlane.Client.Contracts;

public static class ControlPlaneSeverity
{
    public const string Neutral = "neutral";
    public const string Info = "info";
    public const string Success = "success";
    public const string Warning = "warning";
    public const string Error = "error";
}
