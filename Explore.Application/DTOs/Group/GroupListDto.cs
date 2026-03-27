using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Group;

public class GroupListDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }

    public int ApprovalStatusId { get; set; }
    public required string ApprovalStatusFullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public RoleEnum? CurrentUserRole { get; set; }

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
