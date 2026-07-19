// ABOUTME: EF Core mapping for durable notification fanout processor coordination.
// ABOUTME: Enforces one backpressure state row per processor code.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class NotificationFanoutProcessorStateConfiguration
    : IEntityTypeConfiguration<NotificationFanoutProcessorState>
{
    public void Configure(EntityTypeBuilder<NotificationFanoutProcessorState> builder)
    {
        builder.ToTable("notification_fanout_processor_states");
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(state => state.ProcessorCode).HasMaxLength(32).IsRequired();
        builder.HasIndex(state => state.ProcessorCode)
            .IsUnique()
            .HasDatabaseName("ux_notification_fanout_processor_states_processor_code");
    }
}
