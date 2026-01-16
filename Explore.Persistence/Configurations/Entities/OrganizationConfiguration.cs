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

            // Set default value for CreatedAt
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .IsRequired();

            builder.Property(e => e.FullName)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.Email)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.Country)
                .HasMaxLength(200);

            builder.Property(e => e.City)
                .HasMaxLength(200);

            builder.Property(e => e.Address)
                .HasMaxLength(500);

            builder.Property(e => e.Postcode)
                .HasMaxLength(50);

            builder.Property(e => e.WebsiteUrl)
                .HasMaxLength(500);

            builder.HasOne(e => e.ApprovalStatus)
                .WithMany()
                .HasForeignKey(e => e.ApprovalStatusId)
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
                    ActorId = SeedIds.IslamuOrganizationActorId,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
        }
    }
}
