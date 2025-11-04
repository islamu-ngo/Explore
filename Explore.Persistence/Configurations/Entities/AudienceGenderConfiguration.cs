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
                    FullName = "Man"
                },
                new AudienceGender
                {
                    Id = (int)AudienceGenderEnum.Woman,
                    FullName = "Woman"
                },
                new AudienceGender
                {
                    Id = (int)AudienceGenderEnum.Both,
                    FullName = "Both"
                }
                );
        }
    }
}
