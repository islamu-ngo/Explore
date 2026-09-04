// ABOUTME: Configures the composite key and table for embedded Identity user-role memberships.
// ABOUTME: Keeps membership rows normalized and cascade-owned by their Identity user and role.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Identity.Configurations;

public sealed class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
    {
        builder.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
    }
}
