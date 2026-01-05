using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.Email).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FirstName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.LastName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.AuthProvider).HasMaxLength(500);
            builder.Property(e => e.AuthProviderId).HasMaxLength(500);

            builder.HasOne(e => e.Actor)
                .WithMany()
                .HasForeignKey(e => e.ActorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
