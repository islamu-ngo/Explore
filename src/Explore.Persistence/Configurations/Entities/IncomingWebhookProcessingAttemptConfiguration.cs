// ABOUTME: Maps append-only incoming webhook claim and outcome evidence by generation and fence.
// ABOUTME: Bounds failure metadata and keeps each execution event tenant-constrained to its inbox row.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class IncomingWebhookProcessingAttemptConfiguration : IEntityTypeConfiguration<IncomingWebhookProcessingAttempt>
{
    public void Configure(EntityTypeBuilder<IncomingWebhookProcessingAttempt> builder)
    {
        builder.ToTable("incoming_webhook_processing_attempts", table =>
        {
            table.HasCheckConstraint(
                "ck_incoming_webhook_processing_attempts_generation",
                "processing_generation >= 1");
            table.HasCheckConstraint(
                "ck_incoming_webhook_processing_attempts_fence",
                "processing_fence >= 0");
            table.HasCheckConstraint(
                "ck_incoming_webhook_processing_attempts_number",
                "attempt_number >= 0");
        });

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.OutcomeId).IsRequired();
        builder.Ignore(e => e.Outcome);
        builder.Property(e => e.FailureCategory).HasMaxLength(IncomingWebhookMessage.MaxFailureCodeLength);
        builder.Property(e => e.SafeDetail).HasMaxLength(IncomingWebhookMessage.MaxSafeDetailLength);

        builder.HasAlternateKey(e => new { e.TenantId, e.Id })
            .HasName("ak_incoming_webhook_processing_attempts_tenant_id_id");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OutcomeLookup)
            .WithMany()
            .HasForeignKey(e => e.OutcomeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new
        {
            e.TenantId,
            e.IncomingWebhookMessageId,
            e.ProcessingGeneration,
            e.ProcessingFence,
            e.OutcomeId
        })
            .HasDatabaseName("ux_incoming_webhook_processing_attempts_evidence")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.RecordedAt })
            .HasDatabaseName("ix_incoming_webhook_processing_attempts_tenant_recorded");
    }
}
