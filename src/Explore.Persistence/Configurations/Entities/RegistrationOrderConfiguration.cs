// ABOUTME: EF configuration for the tenant-scoped registration order aggregate and immutable snapshots.
// ABOUTME: Maps restrictive tenant-safe relationships so order history cannot be cascade-deleted.

using Explore.Domain;
using Explore.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationOrderConfiguration : IEntityTypeConfiguration<RegistrationOrder>
{
    public void Configure(EntityTypeBuilder<RegistrationOrder> builder)
    {
        builder.ToTable("registration_orders");
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(order => order.GuestAccessTokenHash)
            .HasConversion(hash => hash!.Value, value => CapabilityTokenHash.Create(value))
            .HasMaxLength(44);
        builder.Property(order => order.OrganizerDirectedTotalMinorSnapshot).HasColumnType("bigint");
        builder.Property(order => order.PlatformFeeTotalMinorSnapshot).HasColumnType("bigint");
        builder.Property(order => order.OrganizerEarningsTotalMinorSnapshot).HasColumnType("bigint");
        builder.Property(order => order.PlatformContributionTotalMinorSnapshot).HasColumnType("bigint");
        builder.Property(order => order.TotalDueMinorSnapshot).HasColumnType("bigint");
        builder.Property(order => order.CreatedAt).IsRequired();
        builder.Property(order => order.IsDeleted).HasDefaultValue(false);
        builder.Property(order => order.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(order => new { order.TenantId, order.Id });

        builder.OwnsOne(order => order.ParticipationSnapshot, snapshot =>
        {
            snapshot.Property(value => value.ConfigurationVersion).HasColumnName("participation_configuration_version_snapshot");
            snapshot.Property(value => value.ParticipationHandlingModeId).HasColumnName("participation_handling_mode_id_snapshot");
            snapshot.Property(value => value.AdvanceRegistrationObligationId).HasColumnName("advance_registration_obligation_id_snapshot");
            snapshot.Property(value => value.IdentityAccessModeId).HasColumnName("identity_access_mode_id_snapshot");
            snapshot.Property(value => value.GuestRecoveryPolicy).HasColumnName("guest_recovery_policy_snapshot");
        });

        builder.HasOne<Tenant>().WithMany().HasForeignKey(order => order.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany().HasForeignKey(order => new { order.TenantId, order.EventId })
            .HasPrincipalKey(@event => new { @event.TenantId, @event.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventTicketCatalogVersion>().WithMany().HasForeignKey(order => new { order.TenantId, order.TicketCatalogVersionId })
            .HasPrincipalKey(catalog => new { catalog.TenantId, catalog.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(order => order.BookingPartyType).WithMany().HasForeignKey(order => order.BookingPartyTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(order => order.RegistrationOrderStatus).WithMany().HasForeignKey(order => order.RegistrationOrderStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(order => order.Lines).WithOne().HasForeignKey(line => new { line.TenantId, line.RegistrationOrderId })
            .HasPrincipalKey(order => new { order.TenantId, order.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(order => order.Pii).WithOne(pii => pii.RegistrationOrder).HasForeignKey<RegistrationOrderPii>(pii => new { pii.TenantId, pii.RegistrationOrderId })
            .HasPrincipalKey<RegistrationOrder>(order => new { order.TenantId, order.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(order => order.PlatformContribution).WithOne().HasForeignKey<RegistrationOrderPlatformContribution>(contribution => new { contribution.TenantId, contribution.RegistrationOrderId })
            .HasPrincipalKey<RegistrationOrder>(order => new { order.TenantId, order.Id }).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(order => new { order.TenantId, order.EventId, order.RegistrationOrderStatusId });
        builder.HasIndex(order => new { order.TenantId, order.ExpiresAt });
    }
}
