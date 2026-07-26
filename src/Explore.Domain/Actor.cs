// ABOUTME: Domain entity representing an actor in the system.
// An actor can be either a User or an Organization and is the entity that performs actions.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class Actor : IAuditableEntity, ISoftDeletable, IConcurrencyAware
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

    [ForeignKey(nameof(ExternalActorSubjectId))]
    public Guid? ExternalActorSubjectId { get; set; }
    public ExternalActorSubject? ExternalActorSubject { get; set; }

    [ForeignKey(nameof(ServicePrincipalId))]
    public Guid? ServicePrincipalId { get; set; }
    public ServicePrincipal? ServicePrincipal { get; set; }

    public ICollection<AtprotoIdentity> AtprotoIdentities { get; set; } = [];
    public ICollection<ActorModerationRecord> ModerationRecords { get; set; } = [];
    public ICollection<ActorMerge> MergesFrom { get; set; } = [];
    public ICollection<ActorMerge> MergesInto { get; set; } = [];

    public bool IsSuspended { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public Guid? SuspendedBy { get; set; }
    public string? ModerationReasonCode { get; set; }

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

    public string? Description { get; set; }
    public string? ProfilePictureCid { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BackgroundEffect { get; set; }
    public string? BannerColor { get; set; }

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
    public Guid ConcurrencyStamp { get; set; }

}
