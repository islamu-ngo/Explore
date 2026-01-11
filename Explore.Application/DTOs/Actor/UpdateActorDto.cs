using System;

namespace Explore.Application.DTOs.Actor
{
    /// <summary>
    /// Update Actor payload (Id required)
    /// Used for PUT /api/v1/actor/{id}
    /// Note: UserId and OrganizationId cannot be changed after creation.
    /// The Actor's ownership (User or Organization) is immutable.
    /// </summary>
    public class UpdateActorDto
    {
        public Guid Id { get; set; }

        public int ActorTypeId { get; set; }

        public Guid TenantId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public Guid? ProfilePictureId { get; set; }

        // Federation identifiers
        public string? Did { get; set; }
        public string? Handle { get; set; }

        public int? DidCustodyTypeId { get; set; }

        // Federation metadata
        public string? PdsHost { get; set; }
        public string? Description { get; set; }
        public DateTime? IndexedAt { get; set; }

        // Content addressing
        public string? ProfilePictureCid { get; set; }
        public string? ProfilePictureUri { get; set; }
    }
}
