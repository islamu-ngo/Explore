// ABOUTME: EF Core configuration for SecretBinding with normalized metadata columns,
// ABOUTME: CHECK constraints enforcing source-type/metadata consistency, and filtered unique indexes for Postgres NULL semantics.

namespace Explore.Persistence.Configurations.Entities;

using Explore.Domain.Secrets;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SecretBindingConfiguration : IEntityTypeConfiguration<SecretBinding>
{
    public void Configure(EntityTypeBuilder<SecretBinding> builder)
    {
        builder.HasKey(e => e.Id);

        // UUID v7 generation for better index performance
        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.SettingKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Scope)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ScopeId);

        builder.Property(e => e.SourceType)
            .IsRequired()
            .HasConversion<int>();

        // Normalized metadata columns. Exactly one group is populated per row (enforced by CHECK below).
        builder.Property(e => e.InfisicalEnvironment).HasMaxLength(64);
        builder.Property(e => e.InfisicalPath).HasMaxLength(512);
        builder.Property(e => e.InfisicalKey).HasMaxLength(256);
        builder.Property(e => e.EnvironmentVariableName).HasMaxLength(256);
        builder.Property(e => e.InlineCiphertext);
        builder.Property(e => e.InlineCiphertextVersion);

        builder.Property(e => e.IsLocked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.LastValidationResult)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(Explore.Domain.Enums.SecretValidationResult.NotValidated);

        builder.Property(e => e.LastValidationError)
            .HasMaxLength(1000);

        builder.Property(e => e.LastValidatedAt);
        builder.Property(e => e.LastValidatedBy);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.CreatedBy);
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.UpdatedBy);

        // Filtered unique indexes for Postgres NULL semantics (Oracle-mandated for correct uniqueness on nullable ScopeId):
        // - Instance rows: one binding per (SettingKey) where scope_id IS NULL
        builder.HasIndex(e => e.SettingKey)
            .IsUnique()
            .HasFilter("scope_id IS NULL")
            .HasDatabaseName("ix_secret_bindings_setting_key_instance_unique");

        // - Tenant rows: one binding per (SettingKey, ScopeId) where scope_id IS NOT NULL
        builder.HasIndex(e => new { e.SettingKey, e.ScopeId })
            .IsUnique()
            .HasFilter("scope_id IS NOT NULL")
            .HasDatabaseName("ix_secret_bindings_setting_key_scope_id_tenant_unique");

        // Lookup index by scope for bulk listing
        builder.HasIndex(e => new { e.Scope, e.ScopeId })
            .HasDatabaseName("ix_secret_bindings_scope_scope_id");

        builder.ToTable(t =>
        {
            // CHECK: scope/scope_id consistency (Instance=0 requires ScopeId NULL, Tenant=1 requires ScopeId NOT NULL).
            t.HasCheckConstraint(
                "ck_secret_bindings_scope_scope_id",
                "(scope = 0 AND scope_id IS NULL) OR (scope = 1 AND scope_id IS NOT NULL)");

            // CHECK: exactly one metadata group populated per SourceType.
            //  SourceType 0 (Infisical): env/path/key all NOT NULL, other groups NULL.
            //  SourceType 1 (InlineEncrypted): ciphertext + version NOT NULL, other groups NULL.
            //  SourceType 2 (EnvironmentVariable): variable_name NOT NULL, other groups NULL.
            t.HasCheckConstraint(
                "ck_secret_bindings_source_metadata",
                "(source_type = 0 AND infisical_environment IS NOT NULL AND infisical_path IS NOT NULL AND infisical_key IS NOT NULL " +
                "  AND environment_variable_name IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL) " +
                "OR (source_type = 1 AND inline_ciphertext IS NOT NULL AND inline_ciphertext_version IS NOT NULL " +
                "  AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND environment_variable_name IS NULL) " +
                "OR (source_type = 2 AND environment_variable_name IS NOT NULL " +
                "  AND infisical_environment IS NULL AND infisical_path IS NULL AND infisical_key IS NULL AND inline_ciphertext IS NULL AND inline_ciphertext_version IS NULL)");
        });
    }
}
