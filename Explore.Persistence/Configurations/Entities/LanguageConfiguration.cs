using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class LanguageConfiguration : IEntityTypeConfiguration<Language>
    {
        public void Configure(EntityTypeBuilder<Language> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new Language { Id = 1, MasterCode = "AR", FullName = "Arabic", Description = "Arabic language" },
                new Language { Id = 2, MasterCode = "EN", FullName = "English", Description = "English language" },
                new Language { Id = 3, MasterCode = "FR", FullName = "French", Description = "French language" },
                new Language { Id = 4, MasterCode = "TR", FullName = "Turkish", Description = "Turkish language" },
                new Language { Id = 5, MasterCode = "UR", FullName = "Urdu", Description = "Urdu language" },
                new Language { Id = 6, MasterCode = "ID", FullName = "Indonesian", Description = "Indonesian language" },
                new Language { Id = 7, MasterCode = "MS", FullName = "Malay", Description = "Malay language" },
                new Language { Id = 8, MasterCode = "BN", FullName = "Bengali", Description = "Bengali language" },
                new Language { Id = 9, MasterCode = "FA", FullName = "Persian", Description = "Persian/Farsi language" },
                new Language { Id = 10, MasterCode = "DE", FullName = "German", Description = "German language" },
                new Language { Id = 11, MasterCode = "NL", FullName = "Dutch", Description = "Dutch language" },
                new Language { Id = 12, MasterCode = "ES", FullName = "Spanish", Description = "Spanish language" });
        }
    }
}
