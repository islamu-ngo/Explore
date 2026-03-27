using System;

namespace Explore.Application.DTOs.Actor;

/// <summary>
/// Full Actor details with navigation properties
/// Used for GET /api/actor/{id}
/// </summary>
public class ActorDto
{
    public Guid Id { get; set; }

    // ActorType relationship with i18n support
    public int ActorTypeId { get; set; }
    public string? ActorTypeMasterCode { get; set; } // For i18n with Tolgee
    public string? ActorTypeFullName { get; set; } // Fallback default

    public Guid TenantId { get; set; }

    /// <summary>
    /// The User ID this Actor belongs to (if User actor).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// The Organization ID this Actor belongs to (if Organization actor).
    /// </summary>
    public Guid? OrganizationId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    // ProfilePicture relationship (optional)
    public Guid? ProfilePictureId { get; set; }
    public string? ProfilePictureCid { get; set; }
    public string? ProfilePictureUri { get; set; }

    // Federation identifiers (ATProto/ActivityPub)
    public string? Did { get; set; } // Decentralized identifier (e.g., did:plc:xxx)
    public string? Handle { get; set; } // Human-readable handle (e.g., user.bsky.social)

    // DidCustodyType relationship with i18n support (optional)
    public int? DidCustodyTypeId { get; set; }
    public string? DidCustodyTypeMasterCode { get; set; } // For i18n with Tolgee
    public string? DidCustodyTypeFullName { get; set; } // Fallback default

    // Appearance
    public string? BackgroundColor { get; set; }
    public string? BackgroundEffect { get; set; }
    public string? BannerColor { get; set; }
    public Guid? BannerPictureId { get; set; }
    public string? BannerPictureUri { get; set; }
    public Guid? BackgroundImageId { get; set; }
    public string? BackgroundImageUri { get; set; }

    // Federation metadata
    public string? PdsHost { get; set; } // Personal Data Server host
    public string? Description { get; set; }
    public DateTime? IndexedAt { get; set; }
}
