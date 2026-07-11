using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class AudienceAgeConfiguration : IEntityTypeConfiguration<AudienceAge>
{
    public void Configure(EntityTypeBuilder<AudienceAge> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();

    }
}

