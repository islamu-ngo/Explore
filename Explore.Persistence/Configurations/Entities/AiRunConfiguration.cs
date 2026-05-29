// ABOUTME: EF Core mapping for AI provider run audit rows.
// ABOUTME: Indexes pending runs by tenant/provider while bounding provider failure metadata.

using Explore.Domain.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class AiRunConfiguration : IEntityTypeConfiguration<AiRun>
{
    public void Configure(EntityTypeBuilder<AiRun> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Status).HasConversion<int>().IsRequired();
        builder.Property(e => e.Provider).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ModelId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.FailureCode).HasMaxLength(100);
        builder.Property(e => e.FailureMessage).HasMaxLength(1000);

        builder.HasIndex(e => new { e.TenantId, e.Status, e.QueuedAt })
            .HasDatabaseName("ix_ai_runs_tenant_status_queued_at");

        builder.HasIndex(e => new { e.TenantId, e.ConversationId, e.QueuedAt })
            .HasDatabaseName("ix_ai_runs_tenant_conversation_queued_at");

        builder.ToTable(t =>
            t.HasCheckConstraint("ck_ai_runs_status", "status IN (1, 2, 3, 4, 5)"));
    }
}
