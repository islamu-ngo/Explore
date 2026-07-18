// ABOUTME: Maps tenant-specific visibility decisions for globally canonical AT Protocol records.
// ABOUTME: Enforces one presentation row per tenant and record with same-tenant query filtering.

using Explore.Domain.Federation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities.Federation;

public sealed class AtprotoRecordTenantPresentationConfiguration
    : IEntityTypeConfiguration<AtprotoRecordTenantPresentation>
{
    public void Configure(EntityTypeBuilder<AtprotoRecordTenantPresentation> builder)
    {
        builder.ToTable("atproto_record_tenant_presentations", table =>
            table.HasCheckConstraint(
                "ck_atproto_record_tenant_presentations_source_version",
                "source_version >= 0"));
        builder.HasKey(value => new { value.TenantId, value.AtprotoRecordId });
        builder.HasOne(value => value.Tenant)
            .WithMany()
            .HasForeignKey(value => value.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(value => value.AtprotoRecord)
            .WithMany()
            .HasForeignKey(value => value.AtprotoRecordId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(value => new { value.TenantId, value.IsVisible, value.EvaluatedAt })
            .HasDatabaseName("ix_atproto_record_presentations_visible");
    }
}
