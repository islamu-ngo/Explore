// ABOUTME: EF Core configuration for EventContactShareConsent entity — per-organizer consent records.
// ABOUTME: Defines unique scope index (tenant+user+actor+purpose), FK behaviour, and column constraints.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventContactShareConsentConfiguration : IEntityTypeConfiguration<EventContactShareConsent>
{
    public void Configure(EntityTypeBuilder<EventContactShareConsent> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        // Column constraints
        builder.Property(e => e.EmailSnapshot).HasMaxLength(320);
        builder.Property(e => e.EmailNormalizedSnapshot).HasMaxLength(320);
        builder.Property(e => e.PurposeCode).HasMaxLength(100);
        builder.Property(e => e.ConsentUiVersion).HasMaxLength(100);

        // FK behaviour
        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.SourceEvent)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.SourceEventId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.RecipientActor)
            .WithMany()
            .HasForeignKey(e => e.RecipientActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.SourceRegistrationOrder)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.SourceRegistrationOrderId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index: one consent per tenant + user + recipient actor + purpose
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.RecipientActorId, e.PurposeCode })
            .IsUnique()
            .HasDatabaseName("ix_eventcontactshareconsents_scope_unique");

        // Query index: organiser fetching granted consents for their org
        builder.HasIndex(e => new { e.TenantId, e.RecipientActorId, e.Status })
            .HasDatabaseName("ix_eventcontactshareconsents_recipient_status");

        // Query index: user viewing their own consents
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.Status })
            .HasDatabaseName("ix_eventcontactshareconsents_user_status");
    }
}
