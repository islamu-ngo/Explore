// ABOUTME: EF Core configuration for tenant administrator user-role mappings.
// ABOUTME: Enforces unique assignment per user and tenant with role lookup linkage.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class TenantAdministratorConfiguration : IEntityTypeConfiguration<TenantAdministrator>
{
    public void Configure(EntityTypeBuilder<TenantAdministrator> builder)
    {
        builder.ToTable("TenantAdministrators");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.GrantedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(e => new { e.TenantId, e.UserId })
            .IsUnique();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TenantAdministratorRole)
            .WithMany()
            .HasForeignKey(e => e.TenantAdministratorRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
