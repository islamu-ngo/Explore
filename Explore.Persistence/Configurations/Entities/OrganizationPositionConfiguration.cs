using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class OrganizationPositionConfiguration : IEntityTypeConfiguration<OrganizationPosition>
    {
        public void Configure(EntityTypeBuilder<OrganizationPosition> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Founder, MasterCode = "FOUNDER", FullName = "Founder", Description = "Organization founder" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Director, MasterCode = "DIRECTOR", FullName = "Director", Description = "Organization director" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Manager, MasterCode = "MANAGER", FullName = "Manager", Description = "Organization manager" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Teacher, MasterCode = "TEACHER", FullName = "Teacher", Description = "Teacher or instructor" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Secretary, MasterCode = "SECRETARY", FullName = "Secretary", Description = "Organization secretary" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Treasurer, MasterCode = "TREASURER", FullName = "Treasurer", Description = "Organization treasurer" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Coordinator, MasterCode = "COORDINATOR", FullName = "Coordinator", Description = "Event or activity coordinator" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Volunteer, MasterCode = "VOLUNTEER", FullName = "Volunteer", Description = "Organization volunteer" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Intern, MasterCode = "INTERN", FullName = "Intern", Description = "Organization intern" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Advisor, MasterCode = "ADVISOR", FullName = "Advisor", Description = "Organization advisor" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Consultant, MasterCode = "CONSULTANT", FullName = "Consultant", Description = "Organization consultant" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Supervisor, MasterCode = "SUPERVISOR", FullName = "Supervisor", Description = "Supervisor" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Assistant, MasterCode = "ASSISTANT", FullName = "Assistant", Description = "Assistant" },
                new OrganizationPosition { Id = (int)OrganizationPositionEnum.Staff, MasterCode = "STAFF", FullName = "Staff", Description = "General staff member" });
        }
    }
}
