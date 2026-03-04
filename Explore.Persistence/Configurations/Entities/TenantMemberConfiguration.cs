// ABOUTME: EF Core configuration for TenantMember entity defining FK relationships.
// ABOUTME: Cascade delete on User/Tenant FKs, restrict on Role FK.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class TenantMemberConfiguration : IEntityTypeConfiguration<TenantMember>
{
    public void Configure(EntityTypeBuilder<TenantMember> builder)
    {
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Role)
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one membership per user per tenant
        builder.HasIndex(e => new { e.UserId, e.TenantId })
            .IsUnique()
            .HasDatabaseName("ix_tenantmembers_user_tenant");
    }
}
