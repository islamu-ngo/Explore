// ABOUTME: Describes who owns a secret or sensitive configuration value without exposing the value.
// ABOUTME: Lets UI distinguish application-managed editable secrets from deployment-managed read-only bindings.

namespace Explore.Application.DTOs.Secrets;

public sealed record SecretOwnershipDto
{
    public string Mode { get; init; } = "application-managed";
    public string Source { get; init; } = "application";
    public string Badge { get; init; } = "Managed by Application";
    public string Description { get; init; } = "Stored securely by the platform and editable from Admin UI.";
    public bool Editable { get; init; } = true;
    public bool Configured { get; init; }
    public bool BootstrapAvailable { get; init; }
}
