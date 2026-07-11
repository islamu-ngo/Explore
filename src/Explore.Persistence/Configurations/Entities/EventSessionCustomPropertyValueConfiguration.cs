// ABOUTME: EF Core configuration for session-local typed values with explicit ordinal semantics.
// ABOUTME: Indexes support session-scoped reads and deterministic multi-value ordering.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventSessionCustomPropertyValueConfiguration : IEntityTypeConfiguration<EventSessionCustomPropertyValue>
{
    public void Configure(EntityTypeBuilder<EventSessionCustomPropertyValue> builder)
    {
        builder.ToTable("event_session_custom_property_values");

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.ConcurrencyStamp)
            .IsConcurrencyToken();

        builder.Property(e => e.TextValue)
            .HasMaxLength(4000);

        builder.Property(e => e.NumberValue)
            .HasPrecision(19, 4);

        builder.Property(e => e.Ordinal)
            .HasDefaultValue(0);

        builder.HasOne(e => e.EventSession)
            .WithMany()
            .HasForeignKey(e => e.EventSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Option)
            .WithMany()
            .HasForeignKey(e => e.OptionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.TenantId, e.EventSessionId })
            .HasDatabaseName("ix_escpv_tenant_session");

        builder.HasIndex(e => new { e.EventSessionCustomPropertyDefinitionId, e.EventSessionId, e.Ordinal })
            .HasDatabaseName("ix_escpv_definition_session_ordinal")
            .IsUnique();
    }
}
