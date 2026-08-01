// ABOUTME: Maps immutable registration-form versions with language, provenance, and lifecycle metadata.
// ABOUTME: Enforces composite form ownership, version uniqueness, concurrency, and restrictive history.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationFormVersionConfiguration : IEntityTypeConfiguration<RegistrationFormVersion>
{
    public void Configure(EntityTypeBuilder<RegistrationFormVersion> builder)
    {
        builder.ToTable("registration_form_versions");
        builder.Property(version => version.Id).ValueGeneratedNever();
        builder.Property(version => version.LanguageTag).IsRequired().HasMaxLength(35);
        builder.Property(version => version.SchemaHash).HasMaxLength(128);
        builder.Property(version => version.CreatedAt).IsRequired();
        builder.Property(version => version.IsDeleted).HasDefaultValue(false);
        builder.Property(version => version.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(version => new
        {
            version.TenantId,
            version.EventId,
            version.RegistrationFormId,
            version.Id
        });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(version => version.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany()
            .HasForeignKey(version => new { version.TenantId, version.EventId })
            .HasPrincipalKey(@event => new { @event.TenantId, @event.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RegistrationForm>().WithMany(form => form.Versions)
            .HasForeignKey(version => new { version.TenantId, version.EventId, version.RegistrationFormId })
            .HasPrincipalKey(form => new { form.TenantId, form.EventId, form.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(version => version.Status).WithMany().HasForeignKey(version => version.StatusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(version => new { version.TenantId, version.EventId, version.RegistrationFormId, version.Version })
            .IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(version => new
        {
            version.TenantId,
            version.EventId,
            version.RegistrationFormId,
            version.StatusId,
            version.LanguageTag
        });
    }
}
