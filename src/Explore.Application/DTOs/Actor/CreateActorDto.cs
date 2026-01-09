using System;

namespace Explore.Application.DTOs.Actor
{
    /// <summary>
    /// Create Actor payload (no Id)
    /// Used for POST /api/v1/actor
    /// </summary>
    public class CreateActorDto
    {
        public int ActorTypeId { get; set; }

        public Guid TenantId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public Guid? ProfilePictureId { get; set; }

        // Federation identifiers (optional on creation)
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
