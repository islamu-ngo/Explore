// ABOUTME: EF Core configuration for RolePermission join table with composite PK (RoleId, PermissionId).
// ABOUTME: Links roles to their granted permissions for dynamic RBAC authorization.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        // Composite primary key
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role)
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(rp => rp.GrantedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Fast lookup: all permissions for a role
        builder.HasIndex(rp => rp.RoleId)
            .HasDatabaseName("ix_rolepermissions_role");

        // Fast lookup: all roles with a specific permission
        builder.HasIndex(rp => rp.PermissionId)
            .HasDatabaseName("ix_rolepermissions_permission");
    }
}
