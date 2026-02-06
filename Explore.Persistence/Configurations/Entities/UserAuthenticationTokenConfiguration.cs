using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class UserAuthenticationTokenConfiguration : IEntityTypeConfiguration<UserAuthenticationToken>
    {
        public void Configure(EntityTypeBuilder<UserAuthenticationToken> builder)
        {
            builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

            builder.Property(e => e.Provider).HasMaxLength(500).IsRequired();
            builder.Property(e => e.AccessToken).HasMaxLength(500);
            builder.Property(e => e.RefreshToken).HasMaxLength(500);
            builder.Property(e => e.PdsHost).HasMaxLength(500);
            builder.Property(e => e.DpopKey).HasMaxLength(500);
            builder.Property(e => e.IdToken).HasMaxLength(500);

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
}
