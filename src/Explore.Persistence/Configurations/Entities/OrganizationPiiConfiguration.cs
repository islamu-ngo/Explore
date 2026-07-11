// ABOUTME: Configures the organization_pii extension table with strict 1:1 PK/FK to organizations.
// Stores removable organization-identifying fields separately from core organization lifecycle data.

namespace Explore.Persistence.Configurations.Entities;

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class OrganizationPiiConfiguration : IEntityTypeConfiguration<OrganizationPii>
{
    public void Configure(EntityTypeBuilder<OrganizationPii> builder)
    {
        builder.ToTable("organization_pii");

        builder.HasKey(e => e.OrganizationId);

        builder.Property(e => e.FullName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(320);

        builder.Property(e => e.Country)
            .HasMaxLength(200);

        builder.Property(e => e.City)
            .HasMaxLength(200);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.Postcode)
            .HasMaxLength(50);

        builder.HasIndex(e => e.FullName)
            .HasDatabaseName("ix_organization_pii_name");
    }
}
