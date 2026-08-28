// ABOUTME: EF configuration for the EventSessionKind lookup (talk, workshop, panel, activity, etc.).
// ABOUTME: Ids are assigned manually and seed data is populated at runtime by LookupTableSeeder.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventSessionKindConfiguration : IEntityTypeConfiguration<EventSessionKind>
{
    public void Configure(EntityTypeBuilder<EventSessionKind> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.MasterCode)
            .IsUnique();
    }
}
