using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class RegistrationModeConfiguration : IEntityTypeConfiguration<RegistrationMode>
    {
        public void Configure(EntityTypeBuilder<RegistrationMode> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(50).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new RegistrationMode
                {
                    Id = (int)RegistrationModeEnum.Open,
                    MasterCode = "OPEN",
                    FullName = "Open",
                    Description = "Anyone can register"
                },
                new RegistrationMode
                {
                    Id = (int)RegistrationModeEnum.ApprovalRequired,
                    MasterCode = "APPROVAL_REQUIRED",
                    FullName = "Approval Required",
                    Description = "Registration requires approval"
                },
                new RegistrationMode
                {
                    Id = (int)RegistrationModeEnum.InviteOnly,
                    MasterCode = "INVITE_ONLY",
                    FullName = "Invite Only",
                    Description = "Only invited users can register"
                },
                new RegistrationMode
                {
                    Id = (int)RegistrationModeEnum.Closed,
                    MasterCode = "CLOSED",
                    FullName = "Closed",
                    Description = "Registration is closed"
                });
        }
    }
}
