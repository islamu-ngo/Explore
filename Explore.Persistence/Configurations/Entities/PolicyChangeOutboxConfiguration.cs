// ABOUTME: EF Core configuration for PolicyChangeOutbox — transactional outbox for policy change events.
// ABOUTME: Index on Status+NextRetryAt for efficient background worker polling.

using Explore.Domain.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class PolicyChangeOutboxConfiguration : IEntityTypeConfiguration<PolicyChangeOutbox>
{
    public void Configure(EntityTypeBuilder<PolicyChangeOutbox> builder)
    {
        builder.ToTable("policy_change_outbox");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Scope).HasColumnName("scope");
        builder.Property(x => x.ScopeId).HasColumnName("scope_id");
        builder.Property(x => x.Operation).HasColumnName("operation");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count");
        builder.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000);
        builder.Property(x => x.NextRetryAt).HasColumnName("next_retry_at");

        builder.HasIndex(x => new { x.Status, x.NextRetryAt })
            .HasDatabaseName("ix_policy_change_outbox_status_retry");
    }
}
