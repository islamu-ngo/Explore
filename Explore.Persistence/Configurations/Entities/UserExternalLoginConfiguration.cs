using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
    {
        public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.Provider).HasMaxLength(255);
            builder.Property(e => e.ProviderKey).HasMaxLength(500);
            builder.Property(e => e.ProviderDisplayName).HasMaxLength(500);

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
