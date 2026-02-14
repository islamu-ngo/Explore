// ABOUTME: EF Core configuration for unified Role entity with Scope, IsSystem, and unique MasterCode index.
// ABOUTME: Covers all role scopes (Platform, Tenant, Organization) in a single table.

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

        builder.Property(e => e.Scope)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique MasterCode across all scopes
        builder.HasIndex(e => e.MasterCode)
            .IsUnique()
            .HasDatabaseName("ix_roles_mastercode");

        // Fast lookup by scope (e.g., get all Organization roles)
        builder.HasIndex(e => e.Scope)
            .HasDatabaseName("ix_roles_scope");
    }
}
