// ABOUTME: Sub-resource DTO for instance-level organization policies.
// ABOUTME: Controls organization verification requirements and self-registration.

namespace Explore.Application.DTOs.Instance;

public class OrganizationPolicyDto
{
    public bool RequireOrganizationVerification { get; set; } = true;
    public bool AllowTenantToOmitVerification { get; set; }
    public bool AllowOrganizationSelfRegistration { get; set; } = true;
    public bool AllowGroupSelfRegistration { get; set; } = true;
}
