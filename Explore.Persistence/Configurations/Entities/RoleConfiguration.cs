// ABOUTME: EF Core configuration for unified Role entity with Scope, IsSystem, and unique MasterCode index.
// ABOUTME: Covers all role scopes and exposes alternate keys for scope-constrained grants.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.MasterCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Ignore(e => e.Scope);

        builder.Property(e => e.RoleScopeId)
            .IsRequired();

        builder.HasOne(e => e.RoleScope)
            .WithMany()
            .HasForeignKey(e => e.RoleScopeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique MasterCode across all scopes
        builder.HasIndex(e => e.MasterCode)
            .IsUnique()
            .HasDatabaseName("ix_roles_mastercode");

        builder.HasAlternateKey(e => new { e.Id, e.RoleScopeId })
            .HasName("ak_roles_id_role_scope_id");

        // Fast lookup by scope (e.g., get all Organization roles)
        builder.HasIndex(e => e.RoleScopeId)
            .HasDatabaseName("ix_roles_role_scope_id");
    }
}
