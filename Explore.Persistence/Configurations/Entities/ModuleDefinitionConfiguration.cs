// ABOUTME: EF Core configuration for ModuleDefinition entity.
// ABOUTME: Includes seed data for Core, Islamic, and Tech modules.

using Explore.Domain.Modules;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class ModuleDefinitionConfiguration : IEntityTypeConfiguration<ModuleDefinition>
{
    public void Configure(EntityTypeBuilder<ModuleDefinition> builder)
    {
        builder.ToTable("ModuleDefinitions");

        builder.Property(m => m.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(m => m.ModuleKey).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(500);
        builder.Property(m => m.WizardSchemaUrl).HasMaxLength(500);
        builder.Property(m => m.IconName).HasMaxLength(50);
        builder.Property(m => m.Category).HasMaxLength(50);

        builder.HasIndex(m => m.ModuleKey).IsUnique();
        builder.HasIndex(m => m.DisplayOrder);

        // Seed default modules
        builder.HasData(
            new ModuleDefinition
            {
                Id = SeedIds.ModuleCoreId,
                ModuleKey = "Mod_Core",
                Name = "Core Events",
                Description = "Basic event functionality - title, description, sessions, locations",
                IconName = "Event",
                Category = "Core",
                DisplayOrder = 0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ModuleDefinition
            {
                Id = SeedIds.ModuleIslamicId,
                ModuleKey = "Mod_Islamic",
                Name = "Islamic Events",
                Description = "Islamic-specific features: Madhab selection, prayer time scheduling, gender segregation",
                IconName = "Mosque",
                Category = "Domain",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ModuleDefinition
            {
                Id = SeedIds.ModuleTechId,
                ModuleKey = "Mod_Tech",
                Name = "Tech Events",
                Description = "Developer event features: GitHub repositories, skill levels, live coding sessions",
                IconName = "Code",
                Category = "Domain",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
