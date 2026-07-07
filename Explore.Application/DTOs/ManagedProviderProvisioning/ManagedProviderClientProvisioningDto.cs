// ABOUTME: Request DTO for trusted provider automation that provisions an external customer into the platform.
// ABOUTME: Separates tenant creation, external identity linking, and optional organizer actor creation.

namespace Explore.Application.DTOs.ManagedProviderProvisioning;

public class ManagedProviderClientProvisioningDto
{
    public required string ProviderKey { get; init; }
    public required string ExternalSystem { get; init; }
    public required string ExternalCustomerId { get; init; }
    public required string TenantFullName { get; init; }
    public required string TenantSlug { get; init; }
    public bool ActivateTenant { get; init; } = true;
    public required ManagedProviderExternalAdminDto ExternalAdmin { get; init; }
    public ManagedProviderOrganizerDto? Organizer { get; init; }
}
