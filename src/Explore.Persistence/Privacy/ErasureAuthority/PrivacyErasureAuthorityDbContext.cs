// ABOUTME: Owns only the independently retained platform privacy-erasure authority EF model.
// ABOUTME: Excludes the application model while preserving the shared authority schema lifecycle.

using Explore.Domain;
using Explore.Persistence.Privacy.ErasureAuthority.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Privacy.ErasureAuthority;

public sealed class PrivacyErasureAuthorityDbContext(
    DbContextOptions<PrivacyErasureAuthorityDbContext> options) : DbContext(options)
{
    public DbSet<PrivacyErasureIntent> ErasureIntents => Set<PrivacyErasureIntent>();
    public DbSet<PrivacyErasureCounter> AuthorityCounters => Set<PrivacyErasureCounter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PrivacyErasureAuthorityDatabaseContract.SchemaName);
        modelBuilder.ApplyConfiguration(new PrivacyErasureIntentConfiguration());
        modelBuilder.ApplyConfiguration(new PrivacyErasureCounterConfiguration());
    }
}
