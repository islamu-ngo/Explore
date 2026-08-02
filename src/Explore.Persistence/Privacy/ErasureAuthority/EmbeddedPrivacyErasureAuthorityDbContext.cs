// ABOUTME: Owns the dedicated embedded SQLite privacy-erasure authority model.
// ABOUTME: Keeps retained authority facts isolated from the application database lifecycle.

using Explore.Domain;
using Explore.Persistence.Privacy.ErasureAuthority.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Privacy.ErasureAuthority;

public sealed class EmbeddedPrivacyErasureAuthorityDbContext(
    DbContextOptions<EmbeddedPrivacyErasureAuthorityDbContext> options) : DbContext(options)
{
    public const string MigrationsAssembly = "Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite";
    public const string MigrationsHistoryTable = "__EFPrivacyErasureAuthorityMigrationsHistory";

    public DbSet<PrivacyErasureIntent> ErasureIntents => Set<PrivacyErasureIntent>();
    public DbSet<PrivacyErasureCounter> AuthorityCounters => Set<PrivacyErasureCounter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EmbeddedPrivacyErasureIntentConfiguration());
        modelBuilder.ApplyConfiguration(new EmbeddedPrivacyErasureCounterConfiguration());
    }
}
