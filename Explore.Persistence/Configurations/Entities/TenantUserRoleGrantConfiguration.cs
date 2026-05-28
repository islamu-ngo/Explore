// ABOUTME: EF Core configuration for tenant-local user role grants.
// ABOUTME: Enforces tenant-user ownership, tenant role scope, active-grant uniqueness, and revoke lifecycle.

using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class TenantUserRoleGrantConfiguration : IEntityTypeConfiguration<TenantUserRoleGrant>
{
    public void Configure(EntityTypeBuilder<TenantUserRoleGrant> builder)
    {
        builder.ToTable("tenant_user_role_grants");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.RoleScopeId)
            .IsRequired()
            .HasDefaultValue((int)RoleScopeEnum.Tenant);
        builder.Property(e => e.GrantedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");
        builder.Property(e => e.RevocationReason)
            .HasMaxLength(1000);
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TenantUser)
            .WithMany(e => e.RoleGrants)
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .HasForeignKey(e => new { e.TenantId, e.TenantUserId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Role)
            .WithMany()
            .HasPrincipalKey(e => new { e.Id, e.RoleScopeId })
            .HasForeignKey(e => new { e.RoleId, e.RoleScopeId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasCheckConstraint(
            "ck_tenant_user_role_grants_role_scope",
            $"role_scope_id = {(int)RoleScopeEnum.Tenant}");

        builder.HasIndex(e => new { e.TenantId, e.TenantUserId, e.RoleId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ix_tenant_user_role_grants_active_tenant_user_role");

        builder.HasIndex(e => new { e.TenantId, e.RoleId })
            .HasDatabaseName("ix_tenant_user_role_grants_tenant_role");
    }
}
