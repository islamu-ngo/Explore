// ABOUTME: Describes who owns a secret or sensitive configuration value without exposing the value.
// ABOUTME: Lets UI distinguish application-managed editable secrets from deployment-managed read-only bindings.

namespace Explore.Application.DTOs.Secrets;

public class SecretOwnershipDto
{
    public string Mode { get; set; } = "application-managed";
    public string Source { get; set; } = "application";
    public string Badge { get; set; } = "Managed by Application";
    public string Description { get; set; } = "Stored securely by the platform and editable from Admin UI.";
    public bool Editable { get; set; } = true;
    public bool Configured { get; set; }
    public bool BootstrapAvailable { get; set; }
}
