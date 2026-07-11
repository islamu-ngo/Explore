// ABOUTME: Stores first-run bootstrap state for the platform instance onboarding workflow.
// ABOUTME: Marks when onboarding completes so startup can skip onboarding on subsequent runs.

namespace Explore.Domain;

public class InstanceBootstrapState
{
    public Guid Id { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public string? SelectedDeploymentMode { get; set; }
}
