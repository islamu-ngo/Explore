// ABOUTME: EF Core mapping for tenant-scoped storage upload reservation sessions.
// ABOUTME: Applies expiry/idempotency indexes, provider metadata constraints, and optimistic concurrency.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class StorageUploadSessionConfiguration : IEntityTypeConfiguration<StorageUploadSession>
{
    public void Configure(EntityTypeBuilder<StorageUploadSession> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        builder.Property(e => e.Provider).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(255).IsRequired();
        builder.Property(e => e.OriginalFileName).HasMaxLength(500);
        builder.Property(e => e.SafeDisplayName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Extension).HasMaxLength(50);
        builder.Property(e => e.Purpose).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Visibility).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ObjectKey).HasMaxLength(1024);
        builder.Property(e => e.Sha256Checksum).HasColumnName("sha256_checksum").HasMaxLength(64);
        builder.Property(e => e.IdempotencyKey).HasMaxLength(128);
        builder.Property(e => e.FailureCode).HasMaxLength(100);
        builder.Property(e => e.FailureMessage).HasMaxLength(500);
        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.StorageObject)
            .WithMany()
            .HasForeignKey(e => e.StorageObjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.Status, e.ExpiresAt })
            .HasDatabaseName("ix_storage_upload_sessions_tenant_status_expires_at");

        builder.HasIndex(e => new { e.TenantId, e.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("ux_storage_upload_sessions_tenant_idempotency_key");

        builder.HasIndex(e => new { e.Provider, e.ObjectKey })
            .HasFilter("object_key IS NOT NULL")
            .HasDatabaseName("ix_storage_upload_sessions_provider_object_key");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_storage_upload_sessions_expected_size_nonnegative", "expected_size_bytes >= 0");
            t.HasCheckConstraint("ck_storage_upload_sessions_reserved_bytes_nonnegative", "reserved_bytes >= 0");
            t.HasCheckConstraint("ck_storage_upload_sessions_provider", "provider IN ('local', 's3_compatible', 'legacy_external')");
            t.HasCheckConstraint("ck_storage_upload_sessions_visibility", "visibility IN ('public_image', 'authenticated_tenant', 'private_owner')");
            t.HasCheckConstraint("ck_storage_upload_sessions_purpose", "purpose IN ('legacy_image', 'profile_image', 'event_image', 'attachment', 'document', 'system_asset')");
            t.HasCheckConstraint("ck_storage_upload_sessions_status", "status IN ('reserved', 'uploading', 'finalized', 'canceled', 'failed', 'expired')");
        });
    }
}
