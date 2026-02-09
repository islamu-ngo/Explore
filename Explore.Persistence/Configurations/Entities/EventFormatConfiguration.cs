using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventFormatConfiguration : IEntityTypeConfiguration<EventFormat>
{
    public void Configure(EntityTypeBuilder<EventFormat> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
        builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

    }
}

