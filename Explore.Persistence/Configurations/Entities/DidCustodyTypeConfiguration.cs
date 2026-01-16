using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class DidCustodyTypeConfiguration : IEntityTypeConfiguration<DidCustodyType>
    {
        public void Configure(EntityTypeBuilder<DidCustodyType> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasData(
                new DidCustodyType
                {
                    Id = (int)DidCustodyTypeEnum.Custodial,
                    MasterCode = "CUSTODIAL",
                    FullName = "Custodial",
                    Description = "Platform manages the DID keys"
                },
                new DidCustodyType
                {
                    Id = (int)DidCustodyTypeEnum.SelfCustody,
                    MasterCode = "SELF_CUSTODY",
                    FullName = "Self-Custody",
                    Description = "User manages their own DID keys"
                });
        }
    }
}
