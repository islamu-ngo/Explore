using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class StorageObjectConfiguration : IEntityTypeConfiguration<StorageObject>
    {
        public void Configure(EntityTypeBuilder<StorageObject> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        }
    }
}
