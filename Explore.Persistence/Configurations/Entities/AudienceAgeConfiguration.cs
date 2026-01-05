using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Persistence.Configurations.Entities
{
    public class AudienceAgeConfiguration : IEntityTypeConfiguration<AudienceAge>
    {
        public void Configure(EntityTypeBuilder<AudienceAge> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.HasData(
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.AllAges,
                    MasterCode = "ALL_AGES",
                    FullName = "All Ages",
                    MinAge = null,
                    MaxAge = null
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.AdultsOnly18Plus,
                    MasterCode = "ADULTS_18_PLUS",
                    FullName = "Adults Only (18+)",
                    MinAge = 18,
                    MaxAge = null
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.Teens16Plus,
                    MasterCode = "TEENS_16_PLUS",
                    FullName = "Teens & Adults (16+)",
                    MinAge = 16,
                    MaxAge = null
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.Preteens12Plus,
                    MasterCode = "PRETEENS_12_PLUS",
                    FullName = "Preteens & Up (12+)",
                    MinAge = 12,
                    MaxAge = null
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.ChildrenUnder6,
                    MasterCode = "CHILDREN_UNDER_6",
                    FullName = "Young Children (0-6)",
                    MinAge = null,
                    MaxAge = 6
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.YouthUnder12,
                    MasterCode = "YOUTH_UNDER_12",
                    FullName = "Children (0-12)",
                    MinAge = null,
                    MaxAge = 12
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.YouthUnder16,
                    MasterCode = "YOUTH_UNDER_16",
                    FullName = "Children & Young Teens (0-16)",
                    MinAge = null,
                    MaxAge = 16
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.YouthUnder18,
                    MasterCode = "YOUTH_UNDER_18",
                    FullName = "Youth (0-18)",
                    MinAge = null,
                    MaxAge = 18
                }
            );
        }
    }
}
