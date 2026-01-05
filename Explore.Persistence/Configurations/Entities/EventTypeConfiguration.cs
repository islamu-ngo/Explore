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
            //builder.Property(e => e.Id).ValueGeneratedNever(); i don't know why i put this? doesn't make any sense. so comment and later delete but let commented for now until clear TODO

            builder.HasData(
                new EventType
                {
                    Id = (int)EventTypeEnum.Conference,
                    MasterCode = "CONFERENCE",
                    FullName = "Conference"
                },
                new EventType
                {
                    Id = (int)EventTypeEnum.Webinar,
                    MasterCode = "WEBINAR",
                    FullName = "Webinar"
                },
                new EventType
                {
                    Id = (int)EventTypeEnum.Workshop,
                    MasterCode = "WORKSHOP",
                    FullName = "Workshop"
                });
        }
    }
}
