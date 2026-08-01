// ABOUTME: Maps governed registration fields inside one tenant-scoped form section and version.
// ABOUTME: Enforces composite lineage, stable machine keys, ordinals, constraints, and lookup relationships.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationFormFieldConfiguration : IEntityTypeConfiguration<RegistrationFormField>
{
    public void Configure(EntityTypeBuilder<RegistrationFormField> builder)
    {
        builder.ToTable("registration_form_fields");
        builder.Property(field => field.Id).ValueGeneratedNever();
        builder.Property(field => field.Namespace).IsRequired().HasMaxLength(100);
        builder.Property(field => field.Key).IsRequired().HasMaxLength(100);
        builder.Property(field => field.Label).IsRequired().HasMaxLength(500);
        builder.Property(field => field.RegexPattern).HasMaxLength(1000);
        builder.Property(field => field.AllowedUrlSchemes).HasMaxLength(200);
        builder.Property(field => field.CreatedAt).IsRequired();
        builder.Property(field => field.IsDeleted).HasDefaultValue(false);
        builder.Property(field => field.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(field => new
        {
            field.TenantId,
            field.EventId,
            field.RegistrationFormId,
            field.RegistrationFormVersionId,
            field.RegistrationFormSectionId,
            field.Id
        });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(field => field.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegistrationFormSection>().WithMany(section => section.Fields)
            .HasForeignKey(field => new
            {
                field.TenantId,
                field.EventId,
                field.RegistrationFormId,
                field.RegistrationFormVersionId,
                field.RegistrationFormSectionId
            })
            .HasPrincipalKey(section => new
            {
                section.TenantId,
                section.EventId,
                section.RegistrationFormId,
                section.RegistrationFormVersionId,
                section.Id
            }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(field => field.FieldType).WithMany().HasForeignKey(field => field.FieldTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(field => field.OrganizerVisibility).WithMany().HasForeignKey(field => field.OrganizerVisibilityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(field => new
        {
            field.TenantId,
            field.EventId,
            field.RegistrationFormVersionId,
            field.RegistrationFormSectionId,
            field.Ordinal
        }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(field => new
        {
            field.TenantId,
            field.EventId,
            field.RegistrationFormVersionId,
            field.Namespace,
            field.Key
        }).IsUnique().HasFilter("is_deleted = false");
    }
}
