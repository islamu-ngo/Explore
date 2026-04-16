// ABOUTME: EF configuration for the ScheduleItemKind lookup (Intro, Talk, Q&A, Break, Prayer, Outro, Logistics, Custom).
// ABOUTME: Ids are assigned manually and seed data is populated at runtime by LookupTableSeeder.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class ScheduleItemKindConfiguration : IEntityTypeConfiguration<ScheduleItemKind>
{
    public void Configure(EntityTypeBuilder<ScheduleItemKind> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.MasterCode)
            .HasDatabaseName("ix_schedule_item_kinds_master_code")
            .IsUnique();
    }
}
