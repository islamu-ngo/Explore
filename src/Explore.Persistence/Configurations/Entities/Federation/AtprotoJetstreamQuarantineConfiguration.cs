// ABOUTME: Maps bounded payload-free Jetstream quarantine evidence to its global consumer cursor.
// ABOUTME: Enforces one quarantine outcome per consumer cursor and hash-only record identity evidence.

using Explore.Domain.Federation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities.Federation;

public sealed class AtprotoJetstreamQuarantineConfiguration
    : IEntityTypeConfiguration<AtprotoJetstreamQuarantine>
{
    public void Configure(EntityTypeBuilder<AtprotoJetstreamQuarantine> builder)
    {
        builder.ToTable("atproto_jetstream_quarantines", table =>
        {
            table.HasCheckConstraint("ck_atproto_jetstream_quarantine_cursor", "cursor >= 0");
            table.HasCheckConstraint(
                "ck_atproto_jetstream_quarantine_envelope_hash",
                "envelope_hash ~ '^[0-9a-f]{64}$'");
            table.HasCheckConstraint(
                "ck_atproto_jetstream_quarantine_identity_hash",
                "record_identity_hash IS NULL OR record_identity_hash ~ '^[0-9a-f]{64}$'");
        });
        builder.Property(value => value.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(value => value.ReasonCode).HasMaxLength(100).IsRequired();
        builder.Property(value => value.EnvelopeHash).HasMaxLength(64).IsRequired();
        builder.Property(value => value.RecordIdentityHash).HasMaxLength(64);
        builder.HasOne(value => value.ConsumerState)
            .WithMany()
            .HasForeignKey(value => value.ConsumerStateId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(value => new { value.ConsumerStateId, value.Cursor })
            .IsUnique()
            .HasDatabaseName("ux_atproto_jetstream_quarantine_cursor");
        builder.HasIndex(value => new { value.ReasonCode, value.QuarantinedAt })
            .HasDatabaseName("ix_atproto_jetstream_quarantine_reason");
    }
}
