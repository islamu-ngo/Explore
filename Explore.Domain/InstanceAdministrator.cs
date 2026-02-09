// ABOUTME: Maps users to instance-wide administrative authority for platform governance.
// ABOUTME: Used for onboarding ownership and runtime instance-level settings management.

using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain;

public class InstanceAdministrator
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public required User User { get; set; }

    public DateTime GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }
}
