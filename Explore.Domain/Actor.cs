using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class Actor : ITenantEntity, IAuditableEntity, ISoftDeletable
    {
        public Guid Id { get; set; }
        [ForeignKey("ActorType")]
        public int ActorTypeId { get; set; }
        public ActorType ActorType { get; set; }

        // Navigation Properties & Foreign Keys
        [ForeignKey(nameof(UserId))]
        public Guid? UserId { get; set; }
        public User? User { get; set; }

        [ForeignKey(nameof(OrganizationId))]
        public Guid? OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public string DisplayName { get; set; }

        [ForeignKey("ProfilePictureStorage")]
        public Guid? ProfilePictureId { get; set; }
        public StorageObject? ProfilePicture { get; set; }

        public string? Did { get; set; }
        public string? Handle { get; set; }

        [ForeignKey("DidCustodyType")]
        public int? DidCustodyTypeId { get; set; }
        public DidCustodyType? DidCustodyType { get; set; }

        public string? PdsHost { get; set; }
        public string? Description { get; set; }
        public DateTime? IndexedAt { get; set; }
        public string? ProfilePictureCid { get; set; }
        public string? ProfilePictureUri { get; set; }

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
}
