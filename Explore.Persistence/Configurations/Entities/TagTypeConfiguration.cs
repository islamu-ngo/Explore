using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class TagTypeConfiguration : IEntityTypeConfiguration<TagType>
    {
        public void Configure(EntityTypeBuilder<TagType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new TagType
                {
                    Id = 1,
                    MasterCode = "TOPIC",
                    FullName = "Topic",
                    Description = "Topic-based tags for content categorization"
                },
                new TagType
                {
                    Id = 2,
                    MasterCode = "SKILL",
                    FullName = "Skill Level",
                    Description = "Skill level requirements (beginner, intermediate, advanced)"
                },
                new TagType
                {
                    Id = 3,
                    MasterCode = "LANGUAGE",
                    FullName = "Language",
                    Description = "Language-based tags"
                },
                new TagType
                {
                    Id = 4,
                    MasterCode = "AUDIENCE",
                    FullName = "Audience",
                    Description = "Target audience tags"
                });
        }
    }
}
