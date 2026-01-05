using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class EventFormatConfiguration : IEntityTypeConfiguration<EventFormat>
    {
        public void Configure(EntityTypeBuilder<EventFormat> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new EventFormat
                {
                    Id = (int)EventFormatEnum.Local,
                    MasterCode = "LOCAL",
                    FullName = "Local (In-Person)",
                    Description = "Event takes place at a physical location"
                },
                new EventFormat
                {
                    Id = (int)EventFormatEnum.Digital,
                    MasterCode = "DIGITAL",
                    FullName = "Digital (Online)",
                    Description = "Event takes place online"
                },
                new EventFormat
                {
                    Id = (int)EventFormatEnum.Hybrid,
                    MasterCode = "HYBRID",
                    FullName = "Hybrid",
                    Description = "Event takes place both in-person and online"
                });
        }
    }
}
