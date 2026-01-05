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
                    Id = (int)OrganizationRoles.Admin,
                    MasterCode = OrganizationRoles.Admin.ToString(),
                    FullName = "Administrator",
                    Description = "Organization Administrator with full access"
                },
                new OrganizationRole
                {
                    Id = (int)OrganizationRoles.Moderator,
                    MasterCode = OrganizationRoles.Moderator.ToString(),
                    FullName = "Moderator",
                    Description = "Organization Moderator with limited access"
                },
                new OrganizationRole
                {
                    Id = (int)OrganizationRoles.Member,
                    MasterCode = OrganizationRoles.Member.ToString(),
                    FullName = "Member",
                    Description = "Regular organization member"
                }
            );
        }
    }
}
