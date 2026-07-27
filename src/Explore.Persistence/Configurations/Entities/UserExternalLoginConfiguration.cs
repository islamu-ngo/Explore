// ABOUTME: Configures internal external-login bindings used to resolve one global user per provider identity.
// ABOUTME: Enforces exact provider-key uniqueness before tenant participation is evaluated.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.Provider).HasMaxLength(255);
        builder.Property(e => e.ProviderKey).HasMaxLength(2048);
        builder.Property(e => e.ProviderDisplayName).HasMaxLength(500);

        builder.HasIndex(e => new { e.Provider, e.ProviderKey })
            .IsUnique()
            .HasFilter("provider IS NOT NULL AND provider_key IS NOT NULL");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
