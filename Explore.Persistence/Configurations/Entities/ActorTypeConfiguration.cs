using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class ActorTypeConfiguration : IEntityTypeConfiguration<ActorType>
    {
        public void Configure(EntityTypeBuilder<ActorType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new ActorType
                {
                    Id = (int)ActorTypeEnum.User,
                    MasterCode = "USER",
                    FullName = "User",
                    Description = "Individual user actor"
                },
                new ActorType
                {
                    Id = (int)ActorTypeEnum.Organization,
                    MasterCode = "ORGANIZATION",
                    FullName = "Organization",
                    Description = "Organization actor"
                },
                new ActorType
                {
                    Id = (int)ActorTypeEnum.Bot,
                    MasterCode = "BOT",
                    FullName = "Bot",
                    Description = "Automated bot actor"
                });
        }
    }
}
