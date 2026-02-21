using Explore.Domain;
using Explore.Persistence.Seed;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.AuthProvider).HasMaxLength(500);
        builder.Property(e => e.AuthProviderId).HasMaxLength(500);

        // Make Actor relationship optional at EF level to avoid circular insert issues.
        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Pii)
            .WithOne(e => e.User)
            .HasForeignKey<UserPii>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Pii).AutoInclude();

        // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
        // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
    }
}
