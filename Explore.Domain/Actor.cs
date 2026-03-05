// ABOUTME: Domain entity representing an actor in the system.
// An actor can be either a User or an Organization and is the entity that performs actions.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Actor : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    [ForeignKey("ActorType")]
    public int ActorTypeId { get; set; }

    public required ActorType ActorType { get; set; }

    // Navigation Properties & Foreign Keys
    [ForeignKey(nameof(UserId))]
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    public required Tenant Tenant { get; set; }

    /// <summary>
    /// 1:1 extension table containing actor-identifying fields.
    /// </summary>
    public required ActorPii Pii { get; set; }

    [NotMapped]
    public string DisplayName
    {
        get => Pii.DisplayName;
        set => Pii.DisplayName = value;
    }

    [ForeignKey("ProfilePictureStorage")]
    public Guid? ProfilePictureId { get; set; }
    public StorageObject? ProfilePicture { get; set; }

    [ForeignKey("BannerPictureStorage")]
    public Guid? BannerPictureId { get; set; }
    public StorageObject? BannerPicture { get; set; }

    // Appearance settings
    public string? BackgroundColor { get; set; }
    public string? BackgroundEffect { get; set; }
    public string? BannerColor { get; set; }

    [NotMapped]
    public string? Did
    {
        get => Pii.Did;
        set => Pii.Did = value;
    }

    [NotMapped]
    public string? Handle
    {
        get => Pii.Handle;
        set => Pii.Handle = value;
    }

    [ForeignKey("DidCustodyType")]
    public int? DidCustodyTypeId { get; set; }
    public DidCustodyType? DidCustodyType { get; set; }

    public string? PdsHost { get; set; }
    public string? Description { get; set; }
    public DateTime? IndexedAt { get; set; }
    public string? ProfilePictureCid { get; set; }

    [NotMapped]
    public string? ProfilePictureUri
    {
        get => Pii.ProfilePictureUri;
        set => Pii.ProfilePictureUri = value;
    }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

}
