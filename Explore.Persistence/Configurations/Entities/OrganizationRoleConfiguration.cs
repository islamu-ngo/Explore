using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class OrganizationRoleConfiguration : IEntityTypeConfiguration<OrganizationRole>
    {
        public void Configure(EntityTypeBuilder<OrganizationRole> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new OrganizationRole
                {
                    Id = (int)OrganizationRoleEnum.Creator,
                    MasterCode = "CREATOR",
                    FullName = "Creator",
                    Description = "Organization creator with full ownership"
                },
                new OrganizationRole
                {
                    Id = (int)OrganizationRoleEnum.CoOwner,
                    MasterCode = "CO_OWNER",
                    FullName = "Co-Owner",
                    Description = "Co-owner with near-full access"
                },
                new OrganizationRole
                {
                    Id = (int)OrganizationRoleEnum.Admin,
                    MasterCode = "ADMIN",
                    FullName = "Administrator",
                    Description = "Organization Administrator with management access"
                },
                new OrganizationRole
                {
                    Id = (int)OrganizationRoleEnum.Moderator,
                    MasterCode = "MODERATOR",
                    FullName = "Moderator",
                    Description = "Organization Moderator with limited access"
                },
                new OrganizationRole
                {
                    Id = (int)OrganizationRoleEnum.Member,
                    MasterCode = "MEMBER",
                    FullName = "Member",
                    Description = "Regular organization member"
                },
                new OrganizationRole
                {
                    Id = (int)OrganizationRoleEnum.Viewer,
                    MasterCode = "VIEWER",
                    FullName = "Viewer",
                    Description = "Read-only access to organization"
                }
            );
        }
    }
}
