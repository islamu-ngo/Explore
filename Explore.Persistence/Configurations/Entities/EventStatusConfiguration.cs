using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class EventStatusConfiguration : IEntityTypeConfiguration<EventStatus>
    {
        public void Configure(EntityTypeBuilder<EventStatus> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new EventStatus
                {
                    Id = (int)EventStatusEnum.Draft,
                    MasterCode = "DRAFT",
                    FullName = "Draft",
                    Description = "Event is in draft state and not visible to the public"
                },
                new EventStatus
                {
                    Id = (int)EventStatusEnum.Published,
                    MasterCode = "PUBLISHED",
                    FullName = "Published",
                    Description = "Event is published and visible to the public"
                },
                new EventStatus
                {
                    Id = (int)EventStatusEnum.Cancelled,
                    MasterCode = "CANCELLED",
                    FullName = "Cancelled",
                    Description = "Event has been cancelled"
                },
                new EventStatus
                {
                    Id = (int)EventStatusEnum.Completed,
                    MasterCode = "COMPLETED",
                    FullName = "Completed",
                    Description = "Event has been completed"
                },
                new EventStatus
                {
                    Id = (int)EventStatusEnum.Archived,
                    MasterCode = "ARCHIVED",
                    FullName = "Archived",
                    Description = "Event has been archived"
                });
        }
    }
}
