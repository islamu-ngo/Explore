using Explore.Domain;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class LocationConfiguration : IEntityTypeConfiguration<Location>
    {
        public void Configure(EntityTypeBuilder<Location> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Address).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Postcode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Country).HasMaxLength(500).IsRequired();
            builder.Property(e => e.City).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Timezone).HasMaxLength(500);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasData(
                new Location
                {
                    Id = SeedIds.OnlineLocationId,
                    FullName = "Online / Virtual",
                    Address = "Virtual",
                    Postcode = "00000",
                    Country = "Internet",
                    City = "Virtual",
                    Timezone = "UTC",
                    TenantId = SeedIds.DefaultTenantId
                });
        }
    }
}
