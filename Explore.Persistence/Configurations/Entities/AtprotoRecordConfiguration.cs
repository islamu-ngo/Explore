using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class AtprotoRecordConfiguration : IEntityTypeConfiguration<AtprotoRecord>
    {
        public void Configure(EntityTypeBuilder<AtprotoRecord> builder)
        {
            builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

            builder.Property(e => e.Did).HasMaxLength(255).IsRequired();
            builder.Property(e => e.Collection).HasMaxLength(500).IsRequired();
            builder.Property(e => e.RecordKey).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Cid).HasMaxLength(255);
            builder.Property(e => e.Uri).HasMaxLength(500);

            // Unique constraint on did + collection + record_key
            builder.HasIndex(e => new { e.Did, e.Collection, e.RecordKey }).IsUnique();
        }
    }
}
