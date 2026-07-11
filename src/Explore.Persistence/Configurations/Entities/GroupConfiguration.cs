using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.ApprovalStatusId)
            .HasDefaultValue((int)ApprovalStatusEnum.Pending);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(e => e.FullName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(5000);

        builder.HasOne(e => e.ApprovalStatus)
            .WithMany()
            .HasForeignKey(e => e.ApprovalStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ProfilePicture)
            .WithMany()
            .HasForeignKey(e => e.ProfilePictureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ParentOrganization)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ParentOrganizationId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ParentGroup)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ParentGroupId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasCheckConstraint(
            "ck_groups_parent_exclusive",
            "parent_organization_id IS NULL OR parent_group_id IS NULL");

        builder.HasCheckConstraint(
            "ck_groups_no_self_parent",
            "parent_group_id IS NULL OR parent_group_id <> id");

        builder.HasIndex(e => new { e.TenantId, e.IsDeleted, e.ApprovalStatusId })
            .HasDatabaseName("ix_groups_tenant_active_status");

        builder.HasIndex(e => new { e.TenantId, e.ParentOrganizationId })
            .HasDatabaseName("ix_groups_tenant_parent_organization");

        builder.HasIndex(e => new { e.TenantId, e.ParentGroupId })
            .HasDatabaseName("ix_groups_tenant_parent_group");

        builder.HasIndex(e => new { e.TenantId, e.FullName })
            .HasDatabaseName("ix_groups_tenant_name");

        // Optimistic concurrency control (database-agnostic)
        builder.Property(e => e.ConcurrencyStamp)
            .IsConcurrencyToken();
    }
}
