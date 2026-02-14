using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Organization;

public class OrganizationListDto
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
    public int ApprovalStatusId { get; set; }
    public required string ApprovalStatusFullName { get; set; }
    public string StatusTypeFullName => ApprovalStatusFullName; // Alias for backward compatibility
    public DateTime CreatedAt { get; set; }
    public RoleEnum? CurrentUserRole { get; set; }

    // Profile Picture (resolved to presigned URL)
    public Guid? ActorProfilePictureId { get; set; }
    public string? ActorProfilePictureUri { get; set; }
}
