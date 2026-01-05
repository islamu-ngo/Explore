using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class AudienceGenderConfiguration : IEntityTypeConfiguration<AudienceGender>
    {
        public void Configure(EntityTypeBuilder<AudienceGender> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.HasData(
                new AudienceGender
                {
                    Id = (int)AudienceGenderEnum.Man,
                    MasterCode = "MAN",
                    FullName = "Man",
                    Description = "Only for Man Audience"
                },
                new AudienceGender
                {
                    Id = (int)AudienceGenderEnum.Woman,
                    MasterCode = "WOMAN",
                    FullName = "Woman",
                    Description = "Only for Woman Audience"
                },
                new AudienceGender
                {
                    Id = (int)AudienceGenderEnum.Both,
                    MasterCode = "BOTH_SEGREGATED",
                    FullName = "Both Segregated",
                    Description = "For Both Man and Woman but Segregated so no free mixing"
                },
                new AudienceGender
                {
                    Id = 4,
                    MasterCode = "BOTH_FREE_MIXING",
                    FullName = "Both Free Mixing",
                    Description = "For Both Man and Woman but Free Mixing"
                });
        }
    }
}
