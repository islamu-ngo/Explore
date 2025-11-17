using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class EducationTypeConfiguration : IEntityTypeConfiguration<EducationType>
    {
        public void Configure(EntityTypeBuilder<EducationType> builder)
        {
            builder.HasData(
                new EducationType
                {
                    Id = (int)EducationTypeEnum.School,
                    FullName = "School"
                },
                new EducationType
                {
                    Id = (int)EducationTypeEnum.Institut,
                    FullName = "Institut"
                },
                new EducationType
                {
                    Id = (int)EducationTypeEnum.Course,
                    FullName = "Course"
                });
        }
    }
}
