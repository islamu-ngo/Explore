using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class ProgramRegistrationConfiguration : IEntityTypeConfiguration<ProgramRegistartion>
    {
        public void Configure(EntityTypeBuilder<ProgramRegistartion> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        }
    }
}
