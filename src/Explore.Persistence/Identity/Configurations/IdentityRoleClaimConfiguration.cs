// ABOUTME: Configures the embedded Identity role-claim table and bounded claim columns.
// ABOUTME: Preserves Identity's generated integer claim key and role ownership relationship.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Identity.Configurations;

public sealed class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        builder.HasKey(claim => claim.Id);
        builder.Property(claim => claim.ClaimType).HasMaxLength(256);
        builder.Property(claim => claim.ClaimValue).HasMaxLength(2_048);
    }
}
