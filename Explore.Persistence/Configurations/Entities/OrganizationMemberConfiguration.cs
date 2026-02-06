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
    public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
    {
        public void Configure(EntityTypeBuilder<OrganizationMember> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.HasOne(m => m.Organization)
                .WithMany(o => o.Members)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.OrganizationRole)
                .WithMany()
                .HasForeignKey(m => m.OrganizationRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.OrganizationPosition)
                .WithMany()
                .HasForeignKey(m => m.OrganizationPositionId)
                .OnDelete(DeleteBehavior.Restrict);

            // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
            // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
        }
    }
}
