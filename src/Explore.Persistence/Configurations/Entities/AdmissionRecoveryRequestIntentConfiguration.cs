// ABOUTME: Maps encrypted admission recovery request intents for uniform asynchronous processing.
// ABOUTME: Stores no identity plaintext and applies tenant/concurrency lifecycle controls.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class AdmissionRecoveryRequestIntentConfiguration :
    IEntityTypeConfiguration<AdmissionRecoveryRequestIntent>
{
    public void Configure(EntityTypeBuilder<AdmissionRecoveryRequestIntent> builder)
    {
        builder.ToTable("admission_recovery_request_intents", table =>
        {
            table.HasCheckConstraint(
                "ck_admission_recovery_request_intents_version",
                "protection_version > 0");
        });
        builder.Property(value => value.Id).ValueGeneratedNever();
        builder.Property(value => value.ProtectedIdentity).HasMaxLength(4096).IsRequired();
        builder.Property(value => value.CreatedAt).IsRequired();
        builder.Property(value => value.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(value => new { value.TenantId, value.Id });
        builder.HasIndex(value => new { value.ProcessedAt, value.CreatedAt })
            .HasDatabaseName("ix_admission_recovery_request_intents_pending");
        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(value => value.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
