// ABOUTME: Optional organizer entity payload for provider-provisioned tenants.
// ABOUTME: Creates either an approved Organization actor or an approved Group actor inside the tenant.

namespace Explore.Application.DTOs.ManagedProviderProvisioning;

public sealed record ManagedProviderOrganizerDto
{
    public ManagedProviderOrganizerKindDto Kind { get; init; } = ManagedProviderOrganizerKindDto.Organization;
    public required string FullName { get; init; }
    public string? Email { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? Address { get; init; }
    public string? Postcode { get; init; }
    public string? Description { get; init; }
}
