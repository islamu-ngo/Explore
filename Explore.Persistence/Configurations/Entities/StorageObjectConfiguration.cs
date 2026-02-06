using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class StorageObjectConfiguration : IEntityTypeConfiguration<StorageObject>
    {
        public void Configure(EntityTypeBuilder<StorageObject> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.Uri).HasMaxLength(1000).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Extension).HasMaxLength(50).IsRequired();

            builder.HasOne(e => e.FileType)
                .WithMany()
                .HasForeignKey(e => e.FileTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.SetNull);

            // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
            // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
        }
    }
}
