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
    public class StorageObjectConfiguration : IEntityTypeConfiguration<StorageObject>
    {
        public void Configure(EntityTypeBuilder<StorageObject> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.Uri).HasMaxLength(1000).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Extension).HasMaxLength(50).IsRequired();

            builder.HasOne(e => e.FileType)
                .WithMany()
                .HasForeignKey(e => e.FileTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasData(
                new StorageObject
                {
                    Id = SeedIds.DefaultEventImageId,
                    FileTypeId = (int)FileTypeEnum.Image,
                    Uri = "https://placeholder.islamu.org/event-default.jpg",
                    FullName = "Default Event Image",
                    Extension = ".jpg",
                    Size = 0,
                    TenantId = SeedIds.DefaultTenantId,
                    ActorId = SeedIds.SystemUserActorId
                },
                new StorageObject
                {
                    Id = SeedIds.DefaultProfileImageId,
                    FileTypeId = (int)FileTypeEnum.Image,
                    Uri = "https://placeholder.islamu.org/profile-default.jpg",
                    FullName = "Default Profile Image",
                    Extension = ".jpg",
                    Size = 0,
                    TenantId = SeedIds.DefaultTenantId,
                    ActorId = SeedIds.SystemUserActorId
                },
                new StorageObject
                {
                    Id = SeedIds.DefaultOrganizationLogoId,
                    FileTypeId = (int)FileTypeEnum.Image,
                    Uri = "https://placeholder.islamu.org/org-default.jpg",
                    FullName = "Default Organization Logo",
                    Extension = ".jpg",
                    Size = 0,
                    TenantId = SeedIds.DefaultTenantId,
                    ActorId = SeedIds.SystemUserActorId
                });
        }
    }
}
