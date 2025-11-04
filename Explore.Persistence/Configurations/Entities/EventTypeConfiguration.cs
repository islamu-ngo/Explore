using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class EventTypeConfiguration : IEntityTypeConfiguration<EventType>
    {
        public void Configure(EntityTypeBuilder<EventType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.HasData(
                new EventType
                {
                    Id = (int)EventTypeEnum.Conference,
                    FullName = "Conference"
                }
                ,
                new EventType
                {
                    Id = (int)EventTypeEnum.Webinar,
                    FullName = "Webinar"
                },
                new EventType
                {
                    Id = (int)EventTypeEnum.Workshop,
                    FullName = "Workshop"
                });
        }
    }
}
