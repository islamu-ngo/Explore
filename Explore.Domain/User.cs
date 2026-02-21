using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class User : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; }

    /// <summary>
    /// 1:1 extension table containing sensitive user identity fields.
    /// </summary>
    public UserPii? Pii { get; set; }

    [NotMapped]
    public string Email
    {
        get => Pii?.Email ?? null!;
        set
        {
            EnsurePii();
            Pii!.Email = value;
        }
    }

    [NotMapped]
    public string FirstName
    {
        get => Pii?.FirstName ?? null!;
        set
        {
            EnsurePii();
            Pii!.FirstName = value;
        }
    }

    [NotMapped]
    public string LastName
    {
        get => Pii?.LastName ?? null!;
        set
        {
            EnsurePii();
            Pii!.LastName = value;
        }
    }

    /// <summary>
    /// Every User SHOULD have an associated Actor for identity in the system.
    /// To avoid circular creation issues the ActorId is nullable during creation and
    /// is set immediately after the Actor is created.
    /// </summary>
    [ForeignKey("Actor")]
    public Guid? ActorId { get; set; }
    public Actor? Actor { get; set; }

    public string? AuthProvider { get; set; }
    public string? AuthProviderId { get; set; }
    public Guid? DefaultActorId { get; set; }
    public bool? EmailVerified { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private void EnsurePii()
    {
        Pii ??= new UserPii
        {
            User = this,
            Email = null!,
            FirstName = null!,
            LastName = null!
        };
    }
}
