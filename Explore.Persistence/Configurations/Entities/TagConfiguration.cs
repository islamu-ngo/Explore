using Explore.Domain;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasData(
                new Tag
                {
                    Id = SeedIds.BeginnerTagId,
                    MasterCode = "BEGINNER",
                    FullName = "Beginner",
                    TenantId = SeedIds.DefaultTenantId
                },
                new Tag
                {
                    Id = SeedIds.IntermediateTagId,
                    MasterCode = "INTERMEDIATE",
                    FullName = "Intermediate",
                    TenantId = SeedIds.DefaultTenantId
                },
                new Tag
                {
                    Id = SeedIds.AdvancedTagId,
                    MasterCode = "ADVANCED",
                    FullName = "Advanced",
                    TenantId = SeedIds.DefaultTenantId
                },
                new Tag
                {
                    Id = SeedIds.FreeTagId,
                    MasterCode = "FREE",
                    FullName = "Free",
                    TenantId = SeedIds.DefaultTenantId
                },
                new Tag
                {
                    Id = SeedIds.PaidTagId,
                    MasterCode = "PAID",
                    FullName = "Paid",
                    TenantId = SeedIds.DefaultTenantId
                },
                new Tag
                {
                    Id = SeedIds.OnlineTagId,
                    MasterCode = "ONLINE",
                    FullName = "Online",
                    TenantId = SeedIds.DefaultTenantId
                },
                new Tag
                {
                    Id = SeedIds.InPersonTagId,
                    MasterCode = "IN_PERSON",
                    FullName = "In-Person",
                    TenantId = SeedIds.DefaultTenantId
                });
        }
    }
}
