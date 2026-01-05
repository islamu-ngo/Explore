using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class FileTypeConfiguration : IEntityTypeConfiguration<FileType>
    {
        public void Configure(EntityTypeBuilder<FileType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new FileType
                {
                    Id = (int)FileTypeEnum.Image,
                    MasterCode = "IMAGE",
                    FullName = "Image",
                    Description = "Image file (PNG, JPG, GIF, etc.)"
                },
                new FileType
                {
                    Id = (int)FileTypeEnum.Document,
                    MasterCode = "DOCUMENT",
                    FullName = "Document",
                    Description = "Document file (PDF, DOC, etc.)"
                },
                new FileType
                {
                    Id = (int)FileTypeEnum.Video,
                    MasterCode = "VIDEO",
                    FullName = "Video",
                    Description = "Video file (MP4, AVI, etc.)"
                },
                new FileType
                {
                    Id = (int)FileTypeEnum.Audio,
                    MasterCode = "AUDIO",
                    FullName = "Audio",
                    Description = "Audio file (MP3, WAV, etc.)"
                },
                new FileType
                {
                    Id = (int)FileTypeEnum.Other,
                    MasterCode = "OTHER",
                    FullName = "Other",
                    Description = "Other file type"
                });
        }
    }
}
