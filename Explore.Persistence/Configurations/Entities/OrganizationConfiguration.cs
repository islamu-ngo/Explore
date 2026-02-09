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

        builder.Property(e => e.FullName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Country)
            .HasMaxLength(200);

        builder.Property(e => e.City)
            .HasMaxLength(200);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.Postcode)
            .HasMaxLength(50);

        builder.Property(e => e.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(e => e.MetadataJson)
            .HasColumnType("text");

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

        // ===== Performance Indexes =====

        // Primary listing query: active organizations per tenant with approval status
        builder.HasIndex(e => new { e.TenantId, e.IsDeleted, e.ApprovalStatusId })
            .HasDatabaseName("ix_organizations_tenant_active_status");

        // Organization search by name
        builder.HasIndex(e => new { e.TenantId, e.FullName })
            .HasDatabaseName("ix_organizations_tenant_name");

        // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
        // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
    }
}
