// ABOUTME: EF Core configuration for Permission entity with unique MasterCode and resource kind indexes.
// ABOUTME: Permissions define the vocabulary for dynamic RBAC (resource:action pairs).

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.ResourceKind)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.FieldScope)
            .HasMaxLength(100);

        builder.Property(e => e.MasterCode)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.GroupName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Scope)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IsFiltered)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Unique MasterCode (e.g., "event:update:description")
        builder.HasIndex(e => e.MasterCode)
            .IsUnique()
            .HasDatabaseName("ix_permissions_mastercode");

        // Fast lookup by resource kind (e.g., all "event" permissions)
        builder.HasIndex(e => new { e.ResourceKind, e.Action })
            .HasDatabaseName("ix_permissions_resource_action");

        // Filter by scope for capability ceiling queries
        builder.HasIndex(e => e.Scope)
            .HasDatabaseName("ix_permissions_scope");
    }
}
