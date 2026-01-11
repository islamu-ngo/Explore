using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.Property(e => e.Id)
                .HasDefaultValueSql("uuidv7()");
            builder.Property(e => e.ApprovalStatusId)
                .HasDefaultValue((int)ApprovalStatusEnum.Pending);

            builder.HasData(
                new Organization
                {
                    Id = SeedIds.IslamuOrganizationId,
                    FullName = "ISLAMU",
                    WebsiteUrl = "https://islamu.ngo",
                    Email = "contact@openislamu.org",
                    Country = "Belgium",
                    City = "Brussels",
                    Postcode = "1070",
                    Address = "Parc Du Peterbos",
                    ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
                    TenantId = SeedIds.DefaultTenantId,
                    ActorId = SeedIds.IslamuOrganizationActorId
                });
        }
    }
}
