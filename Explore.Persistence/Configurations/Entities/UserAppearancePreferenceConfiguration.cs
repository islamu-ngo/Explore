// ABOUTME: EF Core configuration for UserAppearancePreference — the active profile selection per user/scope.
// ABOUTME: Unique per (UserId, TenantId) so a user can have different active profiles per tenant.

namespace Explore.Persistence.Configurations.Entities;

using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserAppearancePreferenceConfiguration : IEntityTypeConfiguration<UserAppearancePreference>
{
    public void Configure(EntityTypeBuilder<UserAppearancePreference> builder)
    {
        builder.ToTable("user_appearance_preferences");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("uuidv7()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.TenantId);

        builder.Property(e => e.ActiveProfileId)
            .IsRequired();

        builder.Property(e => e.ThemeMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(AppearanceThemeMode.System);

        builder.Property(e => e.Direction)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("auto");

        builder.Property(e => e.Language)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("en");

        // Unique per user/tenant scope — one active profile per scope
        builder.HasIndex(e => new { e.UserId, e.TenantId })
            .IsUnique();

        builder.HasOne(e => e.ActiveProfile)
            .WithMany()
            .HasForeignKey(e => e.ActiveProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}