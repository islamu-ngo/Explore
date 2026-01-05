using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class IndexedDidConfiguration : IEntityTypeConfiguration<IndexedDid>
    {
        public void Configure(EntityTypeBuilder<IndexedDid> builder)
        {
            builder.HasKey(e => e.Did);

            builder.Property(e => e.Did).HasMaxLength(255).IsRequired();
            builder.Property(e => e.Handle).HasMaxLength(255);
            builder.Property(e => e.PdsHost).HasMaxLength(500).IsRequired();
        }
    }
}
