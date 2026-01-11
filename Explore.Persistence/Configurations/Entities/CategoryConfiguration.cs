using Explore.Domain;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();

            builder.HasOne(e => e.Parent)
                .WithMany()
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasData(
                new Category
                {
                    Id = SeedIds.IslamicStudiesCategoryId,
                    MasterCode = "ISLAMIC_STUDIES",
                    FullName = "Islamic Studies",
                    TenantId = SeedIds.DefaultTenantId,
                    ParentId = null
                },
                new Category
                {
                    Id = SeedIds.QuranCategoryId,
                    MasterCode = "QURAN",
                    FullName = "Quran & Tafsir",
                    TenantId = SeedIds.DefaultTenantId,
                    ParentId = SeedIds.IslamicStudiesCategoryId
                },
                new Category
                {
                    Id = SeedIds.HadithCategoryId,
                    MasterCode = "HADITH",
                    FullName = "Hadith Sciences",
                    TenantId = SeedIds.DefaultTenantId,
                    ParentId = SeedIds.IslamicStudiesCategoryId
                },
                new Category
                {
                    Id = SeedIds.FiqhCategoryId,
                    MasterCode = "FIQH",
                    FullName = "Fiqh (Islamic Jurisprudence)",
                    TenantId = SeedIds.DefaultTenantId,
                    ParentId = SeedIds.IslamicStudiesCategoryId
                },
                new Category
                {
                    Id = SeedIds.AqeedahCategoryId,
                    MasterCode = "AQEEDAH",
                    FullName = "Aqeedah (Islamic Creed)",
                    TenantId = SeedIds.DefaultTenantId,
                    ParentId = SeedIds.IslamicStudiesCategoryId
                },
                new Category
                {
                    Id = SeedIds.SeerahCategoryId,
                    MasterCode = "SEERAH",
                    FullName = "Seerah (Prophetic Biography)",
                    TenantId = SeedIds.DefaultTenantId,
                    ParentId = SeedIds.IslamicStudiesCategoryId
                },
                new Category
                {
                    Id = SeedIds.ArabicLanguageCategoryId,
                    MasterCode = "ARABIC",
                    FullName = "Arabic Language",
                    TenantId = SeedIds.DefaultTenantId,
                    ParentId = null
                },
                new Category
                {
                    Id = SeedIds.CommunityEventsCategoryId,
                    MasterCode = "COMMUNITY",
                    FullName = "Community Events",
                    TenantId = SeedIds.DefaultTenantId,
                    ParentId = null
                });
        }
    }
}
