// ABOUTME: EF Core configuration for instance administrator user mappings.
// ABOUTME: Enforces one unique instance admin assignment per user.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class InstanceAdministratorConfiguration : IEntityTypeConfiguration<InstanceAdministrator>
{
    public void Configure(EntityTypeBuilder<InstanceAdministrator> builder)
    {
        builder.ToTable("InstanceAdministrators");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.GrantedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(e => e.UserId)
            .IsUnique();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
