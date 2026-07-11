// ABOUTME: External identity payload for the tenant administrator created by provider provisioning.
// ABOUTME: Uses stable IdP issuer/provider and subject values instead of mutable email as authority.

namespace Explore.Application.DTOs.ManagedProviderProvisioning;

public class ManagedProviderExternalAdminDto
{
    public required string IdentityProvider { get; init; }
    public required string Subject { get; init; }
    public required string Email { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool EmailVerified { get; init; }
    public string? DisplayName { get; init; }
}
