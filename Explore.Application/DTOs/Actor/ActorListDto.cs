using System;

namespace Explore.Application.DTOs.Actor;

/// <summary>
/// Actor list view with minimal properties
/// Used for GET /api/actor
/// </summary>
public class ActorListDto
{
    public Guid Id { get; set; }

    // ActorType with i18n support
    public int ActorTypeId { get; set; }
    public string? ActorTypeMasterCode { get; set; } // For i18n with Tolgee
    public string? ActorTypeFullName { get; set; } // Fallback default

    public Guid TenantId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    // Federation identifiers
    public string? Did { get; set; }
    public string? Handle { get; set; }

    // DidCustodyType with i18n support (optional)
    public int? DidCustodyTypeId { get; set; }
    public string? DidCustodyTypeMasterCode { get; set; } // For i18n with Tolgee
    public string? DidCustodyTypeFullName { get; set; } // Fallback default

    // ProfilePicture
    public Guid? ProfilePictureId { get; set; }
    public string? ProfilePictureUri { get; set; }

    // Appearance
    public string? BackgroundColor { get; set; }
    public string? BackgroundEffect { get; set; }
    public string? BannerColor { get; set; }
    public Guid? BannerPictureId { get; set; }
    public string? BannerPictureUri { get; set; }
    public Guid? BackgroundImageId { get; set; }
    public string? BackgroundImageUri { get; set; }

    public string? PdsHost { get; set; }
    public DateTime? IndexedAt { get; set; }
}
