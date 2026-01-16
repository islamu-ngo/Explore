using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class VisibilityTypeConfiguration : IEntityTypeConfiguration<VisibilityType>
    {
        public void Configure(EntityTypeBuilder<VisibilityType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new VisibilityType
                {
                    Id = (int)VisibilityTypeEnum.Public,
                    MasterCode = "PUBLIC",
                    FullName = "Public",
                    Description = "Visible to everyone"
                },
                new VisibilityType
                {
                    Id = (int)VisibilityTypeEnum.Private,
                    MasterCode = "PRIVATE",
                    FullName = "Private",
                    Description = "Only visible to invited members"
                },
                new VisibilityType
                {
                    Id = (int)VisibilityTypeEnum.Unlisted,
                    MasterCode = "UNLISTED",
                    FullName = "Unlisted",
                    Description = "Not listed publicly but accessible via direct link"
                },
                new VisibilityType
                {
                    Id = (int)VisibilityTypeEnum.MembersOnly,
                    MasterCode = "MEMBERS_ONLY",
                    FullName = "Members Only",
                    Description = "Only visible to organization members"
                });
        }
    }
}
