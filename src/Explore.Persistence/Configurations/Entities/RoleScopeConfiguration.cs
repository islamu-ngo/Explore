// ABOUTME: EF Core configuration for RBAC role scope lookup values.
// ABOUTME: Uses explicit IDs matching RoleScopeEnum values.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class RoleScopeConfiguration : IEntityTypeConfiguration<RoleScope>
{
    public void Configure(EntityTypeBuilder<RoleScope> builder)
    {
        builder.ToTable("role_scopes");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique();
    }
}
