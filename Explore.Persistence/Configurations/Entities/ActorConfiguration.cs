using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Seed;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class ActorConfiguration : IEntityTypeConfiguration<Actor>
    {
        public void Configure(EntityTypeBuilder<Actor> builder)
        {
            builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

            builder.Property(e => e.DisplayName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Did).HasMaxLength(500);
            builder.Property(e => e.Handle).HasMaxLength(500);
            builder.Property(e => e.PdsHost).HasMaxLength(500);
            builder.Property(e => e.Description).HasMaxLength(500);
            builder.Property(e => e.ProfilePictureCid).HasMaxLength(500);
            builder.Property(e => e.ProfilePictureUri).HasMaxLength(500);

            builder.HasOne(e => e.ActorType)
                .WithMany()
                .HasForeignKey(e => e.ActorTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.DidCustodyType)
                .WithMany()
                .HasForeignKey(e => e.DidCustodyTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ProfilePicture)
                .WithMany()
                .HasForeignKey(e => e.ProfilePictureId)
                .OnDelete(DeleteBehavior.SetNull);

            // User relationship - An Actor can be owned by a User (personal actor)
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Organization relationship - An Actor can be owned by an Organization
            builder.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique indexes to ensure one Actor per User and one per Organization
            builder.HasIndex(e => e.UserId)
                .IsUnique()
                .HasFilter("user_id IS NOT NULL");

            builder.HasIndex(e => e.OrganizationId)
                .IsUnique()
                .HasFilter("organization_id IS NOT NULL");

            // Check constraint: Actor must be either User OR Organization (XOR),
            // OR it can be a Bot (both null for system actors)
            // For User type: UserId must be set, OrganizationId must be null
            // For Organization type: OrganizationId must be set, UserId must be null
            // For Bot type: Both can be null (system actors)
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Actor_UserOrOrganization",
                @"(user_id IS NOT NULL AND organization_id IS NULL) OR " + // User actor
                @"(user_id IS NULL AND organization_id IS NOT NULL)" // Organization actor
            ));

            // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
            // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
        }
    }
}
