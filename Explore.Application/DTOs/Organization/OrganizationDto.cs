using System;

namespace Explore.Application.DTOs.Organization;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public string? WebsiteUrl { get; set; }
    public required string Email { get; set; }
    public required string Country { get; set; }
    public required string City { get; set; }
    public required string Postcode { get; set; }
    public required string Address { get; set; }
    public string? MetadataJson { get; set; }

    // Approval Status
    public int ApprovalStatusId { get; set; }
    public string? ApprovalStatusFullName { get; set; }
    public string? ApprovalStatusMasterCode { get; set; }

    // Tenant
    public Guid TenantId { get; set; }
    public string? TenantFullName { get; set; }

    // Actor
    public Guid? ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? ActorHandle { get; set; }

    // Profile Picture (resolved to presigned URL)
    public Guid? ActorProfilePictureId { get; set; }
    public string? ActorProfilePictureUri { get; set; }
}
