using System;
using Explore.Domain;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Slug).HasMaxLength(500).IsRequired();

            builder.HasIndex(e => e.Slug).IsUnique();

            builder.HasData(
                new Tenant
                {
                    Id = SeedIds.DefaultTenantId,
                    FullName = "ISLAMU Default Tenant",
                    Slug = "default",
                    IsActive = true
                });
        }
    }
}
