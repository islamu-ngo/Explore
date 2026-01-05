using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class ActorConfiguration : IEntityTypeConfiguration<Actor>
    {
        public void Configure(EntityTypeBuilder<Actor> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.DisplayName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Did).HasMaxLength(500);
            builder.Property(e => e.Handle).HasMaxLength(500);
            builder.Property(e => e.PdsHost).HasMaxLength(500);
            builder.Property(e => e.Description).HasMaxLength(500);
            builder.Property(e => e.ProfilePictureCid).HasMaxLength(500);
            builder.Property(e => e.ProfilePictureUri).HasMaxLength(500);

            builder.HasOne(e => e.ActorType)
                .WithMany()
                .HasForeignKey(e => e.ActorTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.DidCustodyType)
                .WithMany()
                .HasForeignKey(e => e.DidCustodyTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ProfilePictureStorage)
                .WithMany()
                .HasForeignKey(e => e.ProfilePictureId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
