// ABOUTME: EF Core configuration for PdsSyncOutbox entity with optimized indexes for background worker queries.
// ABOUTME: Configures UUID v7 generation, string constraints, and filtered indexes for efficient polling.

using Explore.Domain.Federation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities.Federation;

public class PdsSyncOutboxConfiguration : IEntityTypeConfiguration<PdsSyncOutbox>
{
    public void Configure(EntityTypeBuilder<PdsSyncOutbox> builder)
    {
        // Primary key with UUID v7 for time-ordering
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        // AT Protocol identifiers
        builder.Property(e => e.Did).HasMaxLength(255).IsRequired();
        builder.Property(e => e.Collection).HasMaxLength(255).IsRequired();
        builder.Property(e => e.RecordKey).HasMaxLength(255).IsRequired();

        // Operation and status (stored as integers)
        builder.Property(e => e.Operation).IsRequired();
        builder.Property(e => e.Status).IsRequired();

        // Payload stored as JSONB for PostgreSQL efficiency
        builder.Property(e => e.Payload).HasColumnType("jsonb");

        // Optional fields
        builder.Property(e => e.PdsHost).HasMaxLength(255);
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.SourceEntityType).HasMaxLength(100);

        // Dead-letter: default 10 retries before quarantine
        builder.Property(e => e.MaxRetries).HasDefaultValue(10);

        // Primary index for background worker polling: pending items ordered by creation time
        builder.HasIndex(e => new { e.Status, e.NextRetryAt, e.CreatedAt })
            .HasDatabaseName("IX_PdsSyncOutbox_WorkerPoll");

        // Index for DID-based queries (e.g., finding all pending syncs for an actor)
        builder.HasIndex(e => e.Did)
            .HasDatabaseName("IX_PdsSyncOutbox_Did");

        // Index for source entity correlation (debugging and reconciliation)
        builder.HasIndex(e => new { e.SourceEntityType, e.SourceEntityId })
            .HasDatabaseName("IX_PdsSyncOutbox_SourceEntity");

        // Composite unique constraint to prevent duplicate outbox entries
        builder.HasIndex(e => new { e.Did, e.Collection, e.RecordKey, e.Operation, e.CreatedAt })
            .IsUnique()
            .HasDatabaseName("IX_PdsSyncOutbox_Unique");
    }
}
