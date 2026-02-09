using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class ApprovalStatusConfiguration : IEntityTypeConfiguration<ApprovalStatus>
{
    public void Configure(EntityTypeBuilder<ApprovalStatus> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();

    }
}

