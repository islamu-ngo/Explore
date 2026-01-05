using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class ApprovalStatusConfiguration : IEntityTypeConfiguration<ApprovalStatus>
    {
        public void Configure(EntityTypeBuilder<ApprovalStatus> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.HasData(
                new ApprovalStatus
                {
                    Id = (int)ApprovalStatusEnum.Pending,
                    MasterCode = "PENDING",
                    FullName = "Pending",
                    Description = "Status is pending approval of Admin verifying the Existence of Legal Entity"
                },
                new ApprovalStatus
                {
                    Id = (int)ApprovalStatusEnum.Approved,
                    MasterCode = "APPROVED",
                    FullName = "Approved",
                    Description = "Status has been approved by Admin after verifying the Existence of Legal Entity"
                },
                new ApprovalStatus
                {
                    Id = (int)ApprovalStatusEnum.Rejected,
                    MasterCode = "REJECTED",
                    FullName = "Rejected",
                    Description = "Status has been rejected by Admin after failing to verify the Existence of Legal Entity"
                });
        }
    }
}
