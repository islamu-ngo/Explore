// ABOUTME: Organization detail read DTO including profile, actor, status, tenant, and concurrency metadata.
// ABOUTME: The concurrency stamp is returned so clients can issue PATCH requests with If-Match.

using System;

namespace Explore.Application.DTOs.Organization;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public required string FullName { get; set; }
    public string? WebsiteUrl { get; set; }
    public required string Email { get; set; }
    public required string Country { get; set; }
    public required string City { get; set; }
    public required string Postcode { get; set; }
    public required string Address { get; set; }

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
    public string? ActorBackgroundColor { get; set; }
    public string? ActorBackgroundEffect { get; set; }
    public string? ActorBannerColor { get; set; }
    public Guid? ActorBannerPictureId { get; set; }
    public string? ActorBannerPictureUri { get; set; }
    public Guid? ActorBackgroundImageId { get; set; }
    public string? ActorBackgroundImageUri { get; set; }
}
