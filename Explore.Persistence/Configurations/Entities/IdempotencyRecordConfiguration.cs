// ABOUTME: EF Core configuration for IdempotencyRecord entity with unique composite index on (Key, TenantId).
// ABOUTME: Configures UUID v7 generation, column constraints, and ExpiresAt index for cleanup queries.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        // Primary key with UUID v7 for time-ordering
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        // Idempotency key — max 128 characters
        builder.Property(e => e.Key).HasMaxLength(128).IsRequired();

        // Tenant isolation
        builder.Property(e => e.TenantId).IsRequired();

        // Optional user tracking
        builder.Property(e => e.UserId).HasMaxLength(256);

        // Cached response data
        builder.Property(e => e.ResponseBody).HasColumnType("text");
        builder.Property(e => e.ContentType).HasMaxLength(256);

        // Unique composite index: same key reusable across tenants
        builder.HasIndex(e => new { e.Key, e.TenantId })
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyRecords_Key_TenantId");

        // Index for cleanup queries (expired record deletion)
        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_IdempotencyRecords_ExpiresAt");
    }
}
