// ABOUTME: Maps tenant-qualified admission recovery lineage and digest-only lifecycle state.
// ABOUTME: Uses provider-portable generation and active-slot uniqueness without filtered indexes.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class AdmissionRecoveryCapabilityConfiguration :
    IEntityTypeConfiguration<AdmissionRecoveryCapability>
{
    public void Configure(EntityTypeBuilder<AdmissionRecoveryCapability> builder)
    {
        builder.ToTable("admission_recovery_capabilities", table =>
        {
            table.HasCheckConstraint(
                "ck_admission_recovery_capabilities_versions",
                "capability_version > 0 AND lookup_key_version > 0");
            table.HasCheckConstraint(
                "ck_admission_recovery_capabilities_lifecycle",
                "(consumed_at IS NULL OR rotated_at IS NULL) AND " +
                "((consumed_at IS NULL AND rotated_at IS NULL AND active_uniqueness_slot = 0) OR " +
                "((consumed_at IS NOT NULL OR rotated_at IS NOT NULL) AND active_uniqueness_slot = capability_version))");
        });
        builder.Property(value => value.Id).ValueGeneratedNever();
        builder.Property(value => value.Purpose).HasMaxLength(40).IsRequired();
        builder.Property(value => value.LookupDigest)
            .HasMaxLength(44)
            .IsFixedLength()
            .IsRequired();
        builder.Property(value => value.LocatorDigest)
            .HasMaxLength(44)
            .IsFixedLength()
            .IsRequired();
        builder.Property(value => value.ExpiresAt).IsRequired();
        builder.Property(value => value.CreatedAt).IsRequired();
        builder.Property(value => value.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(value => new { value.TenantId, value.Id });
        builder.HasIndex(value => new
            {
                value.TenantId,
                value.AdmissionTicketId,
                value.Purpose,
                value.CapabilityVersion
            })
            .HasDatabaseName("ux_admission_recovery_capabilities_generation")
            .IsUnique();
        builder.HasIndex(value => new
            {
                value.TenantId,
                value.LookupKeyVersion,
                value.LookupDigest
            })
            .HasDatabaseName("ux_admission_recovery_capabilities_digest")
            .IsUnique();
        builder.HasIndex(value => new
            {
                value.TenantId,
                value.LookupKeyVersion,
                value.LocatorDigest
            })
            .HasDatabaseName("ux_admission_recovery_capabilities_locator")
            .IsUnique();
        builder.HasIndex(value => new
            {
                value.TenantId,
                value.AdmissionTicketId,
                value.Purpose,
                value.ActiveUniquenessSlot
            })
            .HasDatabaseName("ux_admission_recovery_capabilities_active")
            .IsUnique();
        builder.HasIndex(value => new { value.TenantId, value.RecoveryRequestId, value.Purpose })
            .HasDatabaseName("ix_admission_recovery_capabilities_request");
        builder.HasIndex(value => new { value.ExpiresAt, value.ConsumedAt, value.RotatedAt })
            .HasDatabaseName("ix_admission_recovery_capabilities_expiry");
        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(value => value.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdmissionTicket>().WithMany()
            .HasForeignKey(value => new { value.TenantId, value.AdmissionTicketId })
            .HasPrincipalKey(ticket => new { ticket.TenantId, ticket.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
