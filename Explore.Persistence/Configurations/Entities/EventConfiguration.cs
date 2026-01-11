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
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.UseTptMappingStrategy();

            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
            builder.Property(e => e.TotalViews).HasDefaultValue(0);

            builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(5000);
            builder.Property(e => e.Slug).HasMaxLength(500);
            builder.Property(e => e.CurrencyCode).HasMaxLength(3);
            builder.Property(e => e.EventUrl).HasMaxLength(500);
            builder.Property(e => e.ExternalRegistrationUrl).HasMaxLength(500);
            builder.Property(e => e.Timezone).HasMaxLength(100);

            builder.HasOne(e => e.EventType)
                .WithMany()
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.AudienceGender)
                .WithMany()
                .HasForeignKey(e => e.AudienceGenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.AudienceAge)
                .WithMany()
                .HasForeignKey(e => e.AudienceAgeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.FeaturedImage)
                .WithMany()
                .HasForeignKey(e => e.FeaturedImageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Madhab)
                .WithMany()
                .HasForeignKey(e => e.MadhabId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.VisibilityType)
                .WithMany()
                .HasForeignKey(e => e.VisibilityTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.EventStatus)
                .WithMany()
                .HasForeignKey(e => e.EventStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.EventFormat)
                .WithMany()
                .HasForeignKey(e => e.EventFormatId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.AtprotoRecord)
                .WithMany()
                .HasForeignKey(e => e.AtprotoRecordId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasData(
                new Event
                {
                    Id = SeedIds.SampleEventId,
                    Title = "Welcome to ISLAMU Events",
                    Description = "This is a sample event to demonstrate the ISLAMU Events platform. Feel free to explore and create your own events!",
                    Slug = "welcome-to-islamu-events",
                    EventTypeId = (int)EventTypeEnum.Webinar,
                    AudienceGenderId = (int)AudienceGenderEnum.Both,
                    AudienceAgeId = (int)AudienceAgeEnum.AllAges,
                    ActorId = SeedIds.IslamuOrganizationActorId,
                    Price = 0,
                    CurrencyCode = "EUR",
                    FeaturedImageId = SeedIds.DefaultEventImageId,
                    TotalViews = 0,
                    IsRegistrationRequired = false,
                    MadhabId = null,
                    TenantId = SeedIds.DefaultTenantId,
                    VisibilityTypeId = (int)VisibilityTypeEnum.Public,
                    EventStatusId = (int)EventStatusEnum.Published,
                    EventFormatId = (int)EventFormatEnum.Digital,
                    Timezone = "Europe/Brussels"
                });
        }
    }
}
