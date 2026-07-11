// ABOUTME: EF Core configuration for actor subscription notification level lookup values.
// ABOUTME: Keeps notification policy IDs stable for subscription storage and fanout decisions.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class ActorSubscriptionNotificationLevelConfiguration : IEntityTypeConfiguration<ActorSubscriptionNotificationLevel>
{
    public void Configure(EntityTypeBuilder<ActorSubscriptionNotificationLevel> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.MasterCode)
            .IsUnique()
            .HasDatabaseName("ux_actor_subscription_notification_levels_master_code");
    }
}
