// ABOUTME: Maps append-only incoming webhook redrive provenance across processing generations.
// ABOUTME: Enforces one tenant-scoped redrive record for each target processing generation.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class IncomingWebhookRedriveRecordConfiguration : IEntityTypeConfiguration<IncomingWebhookRedriveRecord>
{
    public void Configure(EntityTypeBuilder<IncomingWebhookRedriveRecord> builder)
    {
        builder.ToTable("incoming_webhook_redrive_records", table =>
        {
            table.HasCheckConstraint(
                "ck_incoming_webhook_redrive_records_generation_order",
                "target_processing_generation > source_processing_generation AND source_processing_generation >= 1");
        });

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.ActorId).HasMaxLength(IncomingWebhookRedriveRecord.MaxActorIdLength).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(IncomingWebhookRedriveRecord.MaxReasonLength).IsRequired();
        builder.Property(e => e.Result).IsRequired();

        builder.HasAlternateKey(e => new { e.TenantId, e.Id })
            .HasName("ak_incoming_webhook_redrive_records_tenant_id_id");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new
        {
            e.TenantId,
            e.IncomingWebhookMessageId,
            e.TargetProcessingGeneration
        })
            .HasDatabaseName("ux_incoming_webhook_redrive_records_target_generation")
            .IsUnique();
    }
}
