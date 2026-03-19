// ABOUTME: EF Core configuration for OrganizationPolicySet — organization-level governance policy overrides.
// ABOUTME: Organizations can only override event policies where the tenant allows it.

using Explore.Domain.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class OrganizationPolicySetConfiguration : IEntityTypeConfiguration<OrganizationPolicySet>
{
    public void Configure(EntityTypeBuilder<OrganizationPolicySet> builder)
    {
        builder.ToTable("organization_policy_sets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrganizationId).HasColumnName("organization_id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasIndex(x => x.OrganizationId).IsUnique();

        builder.OwnsOne(x => x.Events, events =>
        {
            ConfigureSlot(events, e => e.AllowUserSubmittedEvents);
            ConfigureSlot(events, e => e.AllowOrganizationSubmittedEvents);
            ConfigureSlot(events, e => e.AllowGroupSubmittedEvents);
            ConfigureSlot(events, e => e.EventCardClickOpensDetailPage);
            events.ToJson("events_policy");
        });
    }

    private static void ConfigureSlot<TOwner, TPolicy, T>(
        OwnedNavigationBuilder<TOwner, TPolicy> policyBuilder,
        System.Linq.Expressions.Expression<Func<TPolicy, PolicySlot<T>?>> slotSelector)
        where TOwner : class
        where TPolicy : class
    {
        policyBuilder.OwnsOne(slotSelector);
    }
}
