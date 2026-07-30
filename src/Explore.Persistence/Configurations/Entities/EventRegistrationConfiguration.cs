// ABOUTME: EF configuration for concrete session admissions from legacy intents or registration orders.
// ABOUTME: Preserves tenant-safe lineage and makes interim participant linkage nullable.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
        builder.Property(e => e.CoverageEstablishedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventSession)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId, e.EventSessionId })
            .HasPrincipalKey(e => new { e.TenantId, e.EventId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventRegistrationIntent)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId, e.EventRegistrationIntentId })
            .HasPrincipalKey(e => new { e.TenantId, e.EventId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.RegistrationOrder)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.RegistrationOrderId })
            .HasPrincipalKey(order => new { order.TenantId, order.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RegistrationOrderLine)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.RegistrationOrderLineId })
            .HasPrincipalKey(line => new { line.TenantId, line.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.TicketTypeEntitlement)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.TicketTypeEntitlementId })
            .HasPrincipalKey(entitlement => new { entitlement.TenantId, entitlement.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovalStatus)
            .WithMany()
            .HasForeignKey(e => e.ApprovalStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AtprotoRecord)
            .WithMany()
            .HasForeignKey(e => e.AtprotoRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        // ===== Performance Indexes =====

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.EventSessionId, e.UserId })
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ix_eventregistrations_session_user");

        // Registrations by user (my registrations)
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_eventregistrations_user");

        // Children by parent intent (to walk a user's registration intent down to concrete access rows).
        builder.HasIndex(e => new { e.TenantId, e.EventId, e.EventRegistrationIntentId })
            .HasDatabaseName("ix_eventregistrations_intent");

        builder.HasIndex(e => new
            {
                e.TenantId,
                e.RegistrationOrderLineId,
                e.TicketTypeEntitlementId,
                e.EventSessionId,
                e.EntitlementOrdinal
            })
            .IsUnique()
            .HasFilter("registration_order_line_id IS NOT NULL AND ticket_type_entitlement_id IS NOT NULL AND entitlement_ordinal IS NOT NULL AND is_deleted = false")
            .HasDatabaseName("ix_eventregistrations_order_admission");
    }
}
