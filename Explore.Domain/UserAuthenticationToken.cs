using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class UserAuthenticationToken : ITenantEntity, IAuditableEntity
{
    public Guid Id { get; set; }

    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public required User User { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    public required string Provider { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? PdsHost { get; set; }
    public string? DpopKey { get; set; }
    public string? IdToken { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
