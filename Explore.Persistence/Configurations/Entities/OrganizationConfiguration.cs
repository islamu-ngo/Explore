using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Seed;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.ApprovalStatusId)
            .HasDefaultValue((int)ApprovalStatusEnum.Pending);

        // Set default value for CreatedAt
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(e => e.WebsiteUrl)
            .HasMaxLength(2048);

        builder.HasOne(e => e.ApprovalStatus)
            .WithMany()
            .HasForeignKey(e => e.ApprovalStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Pii)
            .WithOne(e => e.Organization)
            .HasForeignKey<OrganizationPii>(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Pii).AutoInclude();

        // ===== Performance Indexes =====

        // Primary listing query: active organizations per tenant with approval status
        builder.HasIndex(e => new { e.TenantId, e.IsDeleted, e.ApprovalStatusId })
            .HasDatabaseName("ix_organizations_tenant_active_status");

        // Organization search by tenant.
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("ix_organizations_tenant");

        // Optimistic concurrency control (database-agnostic)
        builder.Property(e => e.ConcurrencyStamp)
            .IsConcurrencyToken();

        // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
        // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
    }
}
