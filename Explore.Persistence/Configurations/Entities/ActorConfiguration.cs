using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Seed;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class ActorConfiguration : IEntityTypeConfiguration<Actor>
{
    public void Configure(EntityTypeBuilder<Actor> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.PdsHost).HasMaxLength(500);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ProfilePictureCid).HasMaxLength(500);

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

        builder.HasOne(e => e.BannerPicture)
            .WithMany()
            .HasForeignKey(e => e.BannerPictureId)
            .OnDelete(DeleteBehavior.SetNull);

        // Appearance settings
        builder.Property(e => e.BackgroundColor).HasMaxLength(50);
        builder.Property(e => e.BackgroundEffect).HasMaxLength(50);
        builder.Property(e => e.BannerColor).HasMaxLength(50);

        builder.HasOne(e => e.Pii)
            .WithOne(e => e.Actor)
            .HasForeignKey<ActorPii>(e => e.ActorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Pii).AutoInclude();

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

        // Group relationship - An Actor can be owned by a Group
        builder.HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique indexes to ensure one Actor per User and one per Organization
        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");

        builder.HasIndex(e => e.OrganizationId)
            .IsUnique()
            .HasFilter("organization_id IS NOT NULL");

        builder.HasIndex(e => e.GroupId)
            .IsUnique()
            .HasFilter("group_id IS NOT NULL");

        // Check constraint: Actor must be either User OR Organization (XOR),
        // OR Group, OR it can be a Bot (all ownership FKs null).
        // For User type: UserId must be set, OrganizationId/GroupId must be null.
        // For Organization type: OrganizationId must be set, UserId/GroupId must be null.
        // For Group type: GroupId must be set, UserId/OrganizationId must be null.
        // For Bot type: all ownership FKs can be null.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Actor_UserOrOrganization",
            @"(user_id IS NOT NULL AND organization_id IS NULL AND group_id IS NULL) OR " + // User actor
            @"(user_id IS NULL AND organization_id IS NOT NULL AND group_id IS NULL) OR " + // Organization actor
            @"(user_id IS NULL AND organization_id IS NULL AND group_id IS NOT NULL) OR " + // Group actor
            @"(user_id IS NULL AND organization_id IS NULL AND group_id IS NULL)" // Bot actor
        ));

        // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
        // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
    }
}
