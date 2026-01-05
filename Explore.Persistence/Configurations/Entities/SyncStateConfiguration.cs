using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
    {
        public void Configure(EntityTypeBuilder<SyncState> builder)
        {
            builder.Property(e => e.Service).HasMaxLength(500).IsRequired();

            builder.HasIndex(e => e.Service).IsUnique();
        }
    }
}
