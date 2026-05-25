// ABOUTME: EF Core mapping for ExternalBinding with filtered unique indexes for nullable tenant scope.
// ABOUTME: Uses jsonb metadata and CHECK constraints to keep provider/internal identity columns well-formed.

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class ExternalBindingConfiguration : IEntityTypeConfiguration<ExternalBinding>
{
    public void Configure(EntityTypeBuilder<ExternalBinding> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.ProviderKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.ExternalSystem)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.ExternalType)
            .IsRequired()
            .HasMaxLength(128);
        builder.Property(e => e.ExternalId)
            .IsRequired()
            .HasMaxLength(512);
        builder.Property(e => e.InternalType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.ExternalBindingStatusId)
            .IsRequired()
            .HasDefaultValue((int)ExternalBindingStatusEnum.Active);

        builder.Property(e => e.MetadataJson)
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.ScopeTenant)
            .WithMany()
            .HasForeignKey(e => e.ScopeTenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ProviderKey, e.ExternalSystem, e.ExternalType, e.ExternalId })
            .IsUnique()
            .HasFilter("scope_tenant_id IS NULL")
            .HasDatabaseName("ix_external_bindings_external_global_unique");

        builder.HasIndex(e => new { e.ProviderKey, e.ExternalSystem, e.ExternalType, e.ExternalId, e.ScopeTenantId })
            .IsUnique()
            .HasFilter("scope_tenant_id IS NOT NULL")
            .HasDatabaseName("ix_external_bindings_external_tenant_unique");

        builder.HasIndex(e => new { e.ProviderKey, e.ExternalSystem, e.InternalType, e.InternalId })
            .IsUnique()
            .HasFilter("scope_tenant_id IS NULL")
            .HasDatabaseName("ix_external_bindings_internal_global_unique");

        builder.HasIndex(e => new { e.ProviderKey, e.ExternalSystem, e.InternalType, e.InternalId, e.ScopeTenantId })
            .IsUnique()
            .HasFilter("scope_tenant_id IS NOT NULL")
            .HasDatabaseName("ix_external_bindings_internal_tenant_unique");

        builder.HasIndex(e => e.ScopeTenantId)
            .HasDatabaseName("ix_external_bindings_scope_tenant_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_external_bindings_status", "external_binding_status_id IN (1, 2, 3)");
            t.HasCheckConstraint("ck_external_bindings_text_not_blank",
                "length(btrim(provider_key)) > 0 AND length(btrim(external_system)) > 0 AND " +
                "length(btrim(external_type)) > 0 AND length(btrim(external_id)) > 0 AND length(btrim(internal_type)) > 0");
        });
    }
}
