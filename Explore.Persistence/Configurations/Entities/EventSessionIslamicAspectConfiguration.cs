// ABOUTME: Configures the event_session_islamic_aspects extension table for session-level Islamic data.
// Uses strict 1:1 shared primary key with EventSession to enforce vertical partitioning.

namespace Explore.Persistence.Configurations.Entities;

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EventSessionIslamicAspectConfiguration : IEntityTypeConfiguration<EventSessionIslamicAspect>
{
    public void Configure(EntityTypeBuilder<EventSessionIslamicAspect> builder)
    {
        builder.HasKey(e => e.EventSessionId);

        builder.HasOne(e => e.EventSession)
            .WithOne(e => e.IslamicAspect)
            .HasForeignKey<EventSessionIslamicAspect>(e => e.EventSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.StartTimeType)
            .HasConversion<int>()
            .HasDefaultValue(SessionStartTimeType.RelativeToPrayer)
            .IsRequired();

        builder.Property(e => e.ReferencePrayer)
            .HasConversion<int?>();

        builder.Property(e => e.RequiresWudu)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.RitualRequirementsJson)
            .HasColumnType("jsonb");

        builder.ToTable("event_session_islamic_aspects", t => t.HasCheckConstraint(
            "CK_EventSessionIslamicAspect_RelativeStartFields",
            "(start_time_type = 0) OR (start_time_type = 1 AND reference_prayer IS NOT NULL AND offset_minutes IS NOT NULL)"));
    }
}
