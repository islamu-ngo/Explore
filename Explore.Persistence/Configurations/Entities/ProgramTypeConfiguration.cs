using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain.Enums;

namespace Explore.Persistence.Configurations.Entities
{
    public class ProgramTypeConfiguration : IEntityTypeConfiguration<ProgramType>
    {
        public void Configure(EntityTypeBuilder<ProgramType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever(); // Manual ID from data init below (same as enum)

            builder.HasData(
                new ProgramType
                {
                    Id = (int)ProgramTypeEnum.Event,
                    FullName = "Event",
                    Description = "Events like Conferences, Webinars, Workshops & More!"
                },
                new ProgramType
                {
                    Id = (int)ProgramTypeEnum.Education,
                    FullName = "Education",
                    Description = "Educations like Schools, Bootcamps & More!"
                });
        }
    }
}
