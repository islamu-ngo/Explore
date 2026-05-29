// ABOUTME: EF Core mapping for AI-proposed actions that require explicit confirmation before side effects.
// ABOUTME: Stores validated JSON payloads and indexes pending actions for tenant/user workflows.

using Explore.Domain.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class AiProposedActionConfiguration : IEntityTypeConfiguration<AiProposedAction>
{
    public void Configure(EntityTypeBuilder<AiProposedAction> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Kind).HasConversion<int>().IsRequired();
        builder.Property(e => e.Status).HasConversion<int>().IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.FailureCode).HasMaxLength(100);
        builder.Property(e => e.FailureMessage).HasMaxLength(1000);

        builder.HasOne(e => e.Message)
            .WithMany()
            .HasForeignKey(e => e.MessageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.TenantId, e.ConversationId, e.Status, e.CreatedAt })
            .HasDatabaseName("ix_ai_proposed_actions_tenant_conversation_status_created_at");

        builder.HasIndex(e => new { e.TenantId, e.Status, e.Kind, e.CreatedAt })
            .HasDatabaseName("ix_ai_proposed_actions_tenant_status_kind_created_at");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_ai_proposed_actions_kind", "kind IN (1)");
            t.HasCheckConstraint("ck_ai_proposed_actions_status", "status IN (1, 2, 3, 4, 5)");
            t.HasCheckConstraint("ck_ai_proposed_actions_payload_object", "jsonb_typeof(payload_json) = 'object'");
        });
    }
}
