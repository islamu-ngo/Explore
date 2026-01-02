using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.UseTptMappingStrategy();

            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
            builder.Property(e => e.TotalViews).HasDefaultValue(0);
        }
    }
}
