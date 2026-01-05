using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class MadhabConfiguration : IEntityTypeConfiguration<Madhab>
    {
        public void Configure(EntityTypeBuilder<Madhab> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new Madhab
                {
                    Id = (int)MadhabEnum.Hanafi,
                    MasterCode = "HANAFI",
                    FullName = "Hanafi",
                    Description = "Hanafi school of Islamic jurisprudence"
                },
                new Madhab
                {
                    Id = (int)MadhabEnum.Maliki,
                    MasterCode = "MALIKI",
                    FullName = "Maliki",
                    Description = "Maliki school of Islamic jurisprudence"
                },
                new Madhab
                {
                    Id = (int)MadhabEnum.Shafii,
                    MasterCode = "SHAFII",
                    FullName = "Shafi'i",
                    Description = "Shafi'i school of Islamic jurisprudence"
                },
                new Madhab
                {
                    Id = (int)MadhabEnum.Hanbali,
                    MasterCode = "HANBALI",
                    FullName = "Hanbali",
                    Description = "Hanbali school of Islamic jurisprudence"
                },
                new Madhab
                {
                    Id = (int)MadhabEnum.Other,
                    MasterCode = "OTHER",
                    FullName = "Other",
                    Description = "Other Islamic jurisprudence approach"
                });
        }
    }
}
