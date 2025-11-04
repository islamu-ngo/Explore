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
            builder.Property(e => e.Id).ValueGeneratedNever(); // Manual ID from data init below (same as enum)

            builder.HasData(
                // Pas de restriction
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.AllAges,
                    FullName = "All Ages",
                    MinAge = null,
                    MaxAge = null
                },

                // Restrictions "minimum age" (18+, 16+, 12+)
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.AdultsOnly18Plus,
                    FullName = "Adults Only (18+)",
                    MinAge = 18,
                    MaxAge = null
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.Teens16Plus,
                    FullName = "Teens & Adults (16+)",
                    MinAge = 16,
                    MaxAge = null
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.Preteens12Plus,
                    FullName = "Preteens & Up (12+)",
                    MinAge = 12,
                    MaxAge = null
                },

                // Restrictions "maximum age" (pour enfants seulement)
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.ChildrenUnder6,
                    FullName = "Young Children (0-6)",
                    MinAge = null,
                    MaxAge = 6
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.YouthUnder12,
                    FullName = "Children (0-12)",
                    MinAge = null,
                    MaxAge = 12
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.YouthUnder16,
                    FullName = "Children & Young Teens (0-16)",
                    MinAge = null,
                    MaxAge = 16
                },
                new AudienceAge
                {
                    Id = (int)AudienceAgeEnum.YouthUnder18,
                    FullName = "Youth (0-18)",
                    MinAge = null,
                    MaxAge = 18
                }
            );
        }
    }
}
