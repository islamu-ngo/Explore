// ABOUTME: EF configuration for registration-order PII and platform-contribution extension rows.
// ABOUTME: Keeps removable buyer data and platform-directed money separate from the aggregate root table.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationOrderPiiConfiguration : IEntityTypeConfiguration<RegistrationOrderPii>
{
    public void Configure(EntityTypeBuilder<RegistrationOrderPii> builder)
    {
        builder.ToTable("registration_order_pii");
        builder.HasKey(pii => pii.RegistrationOrderId);
        builder.Property(pii => pii.ContactName).HasMaxLength(200);
        builder.Property(pii => pii.Email).HasMaxLength(320);
        builder.Property(pii => pii.NormalizedEmail).HasMaxLength(320);
        builder.Property(pii => pii.Phone).HasMaxLength(64);
        builder.Property(pii => pii.OrganizationName).HasMaxLength(200);
        builder.Property(pii => pii.CreatedAt).IsRequired();
        builder.HasAlternateKey(pii => new { pii.TenantId, pii.RegistrationOrderId });
        builder.HasIndex(pii => new { pii.TenantId, pii.NormalizedEmail });
    }
}

public sealed class RegistrationOrderPlatformContributionConfiguration : IEntityTypeConfiguration<RegistrationOrderPlatformContribution>
{
    public void Configure(EntityTypeBuilder<RegistrationOrderPlatformContribution> builder)
    {
        builder.ToTable("registration_order_platform_contributions");
        builder.Property(contribution => contribution.Id).ValueGeneratedNever();
        builder.Property(contribution => contribution.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(contribution => contribution.AmountMinor).HasColumnType("bigint");
        builder.Property(contribution => contribution.CreatedAt).IsRequired();
        builder.HasAlternateKey(contribution => new { contribution.TenantId, contribution.Id });
        builder.HasIndex(contribution => new { contribution.TenantId, contribution.RegistrationOrderId }).IsUnique();
    }
}
