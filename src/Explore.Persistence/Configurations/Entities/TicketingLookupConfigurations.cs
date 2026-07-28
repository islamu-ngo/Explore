// ABOUTME: EF configurations for stable ticketing lookup rows seeded only by LookupTableSeeder.
// ABOUTME: Maps normalized integer IDs and unique master codes without model HasData rows.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class TicketCatalogStatusConfiguration : LookupConfiguration<TicketCatalogStatus>
{
    protected override string TableName => "ticket_catalog_statuses";
}

public sealed class TicketPricingModeConfiguration : LookupConfiguration<TicketPricingMode>
{
    protected override string TableName => "ticket_pricing_modes";
}

public sealed class ParticipantDataCollectionModeConfiguration : LookupConfiguration<ParticipantDataCollectionMode>
{
    protected override string TableName => "participant_data_collection_modes";
}

public sealed class EntitlementScopeTypeConfiguration : LookupConfiguration<EntitlementScopeType>
{
    protected override string TableName => "entitlement_scope_types";
}

public sealed class EntitlementSelectionRuleConfiguration : LookupConfiguration<EntitlementSelectionRule>
{
    protected override string TableName => "entitlement_selection_rules";
}

public sealed class CapacityOversellPolicyConfiguration : LookupConfiguration<CapacityOversellPolicy>
{
    protected override string TableName => "capacity_oversell_policies";
}

public abstract class LookupConfiguration<TLookup> : IEntityTypeConfiguration<TLookup>
    where TLookup : class
{
    protected abstract string TableName { get; }

    public void Configure(EntityTypeBuilder<TLookup> builder)
    {
        builder.ToTable(TableName);
        builder.Property("Id").ValueGeneratedNever();
        builder.Property("MasterCode").IsRequired().HasMaxLength(100);
        builder.Property("FullName").IsRequired().HasMaxLength(200);
        builder.Property("Description").HasMaxLength(500);
        builder.HasIndex("MasterCode").IsUnique();
    }
}
