using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class ActorKeyStoreConfiguration : IEntityTypeConfiguration<ActorKeyStore>
{
    public void Configure(EntityTypeBuilder<ActorKeyStore> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        builder.Property(e => e.KeyPurpose).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PrivateKeyEncrypted).IsRequired();
        builder.Property(e => e.PublicKey).HasMaxLength(500).IsRequired();

        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
