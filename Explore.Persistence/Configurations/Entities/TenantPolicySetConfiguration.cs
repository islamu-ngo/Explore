// ABOUTME: EF Core configuration for TenantPolicySet — tenant-level governance policy overrides.
// ABOUTME: Each sub-policy section uses table-splitting (flattened columns); only fields with Allow override mode apply.

using Explore.Domain.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class TenantPolicySetConfiguration : IEntityTypeConfiguration<TenantPolicySet>
{
    public void Configure(EntityTypeBuilder<TenantPolicySet> builder)
    {
        builder.ToTable("tenant_policy_sets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasIndex(x => x.TenantId).IsUnique();

        builder.OwnsOne(x => x.Events, events =>
        {
            ConfigureSlot(events, e => e.AllowUserSubmittedEvents);
            ConfigureSlot(events, e => e.AllowOrganizationSubmittedEvents);
            ConfigureSlot(events, e => e.AllowGroupSubmittedEvents);
            ConfigureSlot(events, e => e.EventCardClickOpensDetailPage);
        });

        builder.OwnsOne(x => x.Organizations, orgs =>
        {
            ConfigureSlot(orgs, o => o.RequireVerification);
            ConfigureSlot(orgs, o => o.AllowTenantToOmitVerification);
            ConfigureSlot(orgs, o => o.AllowSelfRegistration);
            ConfigureSlot(orgs, o => o.AllowGroupSelfRegistration);
        });

        builder.OwnsOne(x => x.Branding, branding =>
        {
            ConfigureSlot(branding, b => b.DisplayName);
            ConfigureSlot(branding, b => b.LogoUrl);
            ConfigureSlot(branding, b => b.FaviconUrl);
            ConfigureSlot(branding, b => b.CustomCssUrl);
        });

        builder.OwnsOne(x => x.RenderPolicy, rp =>
        {
            ConfigureSlot(rp, r => r.Version);
            ConfigureSlot(rp, r => r.Preset);
            ConfigureSlot(rp, r => r.EnableAdvancedOverrides);
            ConfigureSlot(rp, r => r.GlobalRenderMode);
            ConfigureSlot(rp, r => r.GlobalPrerenderEnabled);
            ConfigureSlot(rp, r => r.PublicSeoRenderMode);
            ConfigureSlot(rp, r => r.PublicSeoPrerenderEnabled);
            ConfigureSlot(rp, r => r.OperationalRenderMode);
            ConfigureSlot(rp, r => r.OperationalPrerenderEnabled);
            ConfigureSlot(rp, r => r.AdminRenderMode);
            ConfigureSlot(rp, r => r.AdminPrerenderEnabled);
            ConfigureSlot(rp, r => r.OnboardingRenderMode);
            ConfigureSlot(rp, r => r.OnboardingPrerenderEnabled);
            ConfigureSlot(rp, r => r.DisallowInteractiveServerOnOnboarding);
            ConfigureSlot(rp, r => r.AllowTenantOverride);
            ConfigureSlot(rp, r => r.LockTenantPublicSeo);
            ConfigureSlot(rp, r => r.LockTenantOperational);
            ConfigureSlot(rp, r => r.LockTenantAdmin);
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
