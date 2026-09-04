// ABOUTME: Configures composite provider keys for embedded Identity user tokens.
// ABOUTME: Bounds provider and token names while leaving token values protected by their owning service.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Identity.Configurations;

public sealed class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
    {
        builder.HasKey(token => new { token.UserId, token.LoginProvider, token.Name });
        builder.Property(token => token.LoginProvider).HasMaxLength(128);
        builder.Property(token => token.Name).HasMaxLength(128);
    }
}
