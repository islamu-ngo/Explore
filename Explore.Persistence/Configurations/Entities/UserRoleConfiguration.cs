using Explore.Domain;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();

            builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
            builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
            builder.Property(e => e.Description).HasMaxLength(500);

            builder.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasData(
                new UserRole
                {
                    Id = 1,
                    MasterCode = "SUPER_ADMIN",
                    FullName = "Super Administrator",
                    Description = "Full system access across all tenants",
                    TenantId = SeedIds.DefaultTenantId
                },
                new UserRole
                {
                    Id = 2,
                    MasterCode = "ADMIN",
                    FullName = "Administrator",
                    Description = "Tenant administrator with full access within tenant",
                    TenantId = SeedIds.DefaultTenantId
                },
                new UserRole
                {
                    Id = 3,
                    MasterCode = "MODERATOR",
                    FullName = "Moderator",
                    Description = "Content moderation and user management",
                    TenantId = SeedIds.DefaultTenantId
                },
                new UserRole
                {
                    Id = 4,
                    MasterCode = "USER",
                    FullName = "User",
                    Description = "Standard user role",
                    TenantId = SeedIds.DefaultTenantId
                });
        }
    }
}
