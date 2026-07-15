// ABOUTME: Maps tenant-scoped webhook retention holds with normalized subject kinds and cleanup indexes.
// ABOUTME: Preserves independent hold history while allowing efficient active-hold exclusion by subject.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class WebhookRetentionHoldConfiguration : IEntityTypeConfiguration<WebhookRetentionHold>
{
    public void Configure(EntityTypeBuilder<WebhookRetentionHold> builder)
    {
        builder.ToTable("webhook_retention_holds", table =>
        {
            table.HasCheckConstraint(
                "ck_webhook_retention_holds_expiry",
                "expires_at IS NULL OR expires_at > placed_at");
            table.HasCheckConstraint(
                "ck_webhook_retention_holds_release",
                "released_at IS NULL OR released_at >= placed_at");
        });

        builder.Property(hold => hold.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(hold => hold.SubjectKindId).IsRequired();
        builder.Property(hold => hold.ReasonCode)
            .HasMaxLength(WebhookRetentionHold.MaxReasonCodeLength)
            .IsRequired();
        builder.Ignore(hold => hold.SubjectKind);

        builder.HasAlternateKey(hold => new { hold.TenantId, hold.Id })
            .HasName("ak_webhook_retention_holds_tenant_id_id");
        builder.HasOne(hold => hold.Tenant)
            .WithMany()
            .HasForeignKey(hold => hold.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(hold => hold.SubjectKindLookup)
            .WithMany()
            .HasForeignKey(hold => hold.SubjectKindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(hold => new
        {
            hold.TenantId,
            hold.SubjectKindId,
            hold.SubjectId,
            hold.ReleasedAt,
            hold.ExpiresAt
        })
            .HasDatabaseName("ix_webhook_retention_holds_tenant_subject_active");
    }
}
