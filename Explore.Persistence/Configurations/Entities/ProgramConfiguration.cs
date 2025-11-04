using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class ProgramConfiguration : IEntityTypeConfiguration<Program>
    {
        public void Configure(EntityTypeBuilder<Program> builder)
        {
            builder.UseTptMappingStrategy();

            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
            builder.Property(e => e.TotalViews).HasDefaultValue(0);
        }
    }
}
