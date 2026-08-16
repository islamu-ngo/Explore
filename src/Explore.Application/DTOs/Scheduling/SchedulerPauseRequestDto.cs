// ABOUTME: Request body for the instance-wide scheduler pause action.
// ABOUTME: Carries the typed confirmation that guards stopping all background work at once.

namespace Explore.Application.DTOs.Scheduling;

/// <summary>
/// Confirmation for pausing the whole scheduler. The caller must echo the running scheduler's name, which makes
/// the action deliberate rather than a mis-click: pausing stops email dispatch, retention sweeps, and storage
/// reconciliation together, and nothing else in the UI has that blast radius.
/// </summary>
public sealed class SchedulerPauseRequestDto
{
    public string? ConfirmationText { get; set; }
}
