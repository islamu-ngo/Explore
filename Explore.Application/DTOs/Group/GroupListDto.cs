using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Group;

public class GroupListDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
    public int ApprovalStatusId { get; set; }
    public required string ApprovalStatusFullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public RoleEnum? CurrentUserRole { get; set; }

    // Profile Picture (resolved to presigned URL)
    public Guid? ActorProfilePictureId { get; set; }
    public string? ActorProfilePictureUri { get; set; }
}
