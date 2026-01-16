using Explore.Domain;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
    {
        public void Configure(EntityTypeBuilder<TenantSettings> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasData(
                new TenantSettings
                {
                    Id = SeedIds.DefaultTenantSettingsId,
                    TenantId = SeedIds.DefaultTenantId
                });
        }
    }
}
