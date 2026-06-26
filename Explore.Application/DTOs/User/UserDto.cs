using System;

namespace Explore.Application.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    // Actor
    public Guid ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? ActorHandle { get; set; }

    // Actor Appearance
    public string? ActorBackgroundColor { get; set; }
    public string? ActorBackgroundEffect { get; set; }
    public string? ActorBannerColor { get; set; }
    public Guid? ActorBannerPictureId { get; set; }
    public string? ActorBannerPictureUri { get; set; }
    public Guid? ActorBackgroundImageId { get; set; }
    public string? ActorBackgroundImageUri { get; set; }

    // Auth
    public string? AuthProvider { get; set; }
    public string? AuthProviderId { get; set; }
    public bool? EmailVerified { get; set; }

    // Profile image key (S3 object key) and URI for preview
    public string? ProfileImageKey { get; set; }
    public string? ProfileImageUri { get; set; }
}
