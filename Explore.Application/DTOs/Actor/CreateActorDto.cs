using System;

namespace Explore.Application.DTOs.Actor;

/// <summary>
/// Create Actor payload (no Id)
/// Used for POST /api/actor
/// An Actor must be linked to either a User OR an Organization (exactly one, not both, not neither)
/// </summary>
public class CreateActorDto
{
    public int ActorTypeId { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>
    /// The User ID this Actor belongs to. Required if this is a User actor.
    /// Must be null if OrganizationId is set.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// The Organization ID this Actor belongs to. Required if this is an Organization actor.
    /// Must be null if UserId is set.
    /// </summary>
    public Guid? OrganizationId { get; set; }

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
