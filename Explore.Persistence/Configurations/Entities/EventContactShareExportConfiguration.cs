// ABOUTME: EF Core configuration for EventContactShareExport entity — export audit headers.
// ABOUTME: Tracks each time an organisation member downloads shared contact data.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventContactShareExportConfiguration : IEntityTypeConfiguration<EventContactShareExport>
{
    public void Configure(EntityTypeBuilder<EventContactShareExport> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        builder.Property(e => e.Format).HasMaxLength(20);

        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.RecipientActor).WithMany().HasForeignKey(e => e.RecipientActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Event).WithMany().HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ExportedByUser).WithMany().HasForeignKey(e => e.ExportedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.RecipientActorId, e.CreatedAt })
            .HasDatabaseName("ix_eventcontactshareexports_recipient_date");
    }
}
