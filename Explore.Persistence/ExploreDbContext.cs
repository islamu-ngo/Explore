// ABOUTME: Core DbContext for the Explore platform with pooled creation and property-injected scoped services.
// ABOUTME: Split into partial classes: QueryFilters, SaveChanges, DbSets. Hosts the Data Protection key ring.

using Explore.Application.Contracts.Infrastructure;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence;

public partial class ExploreDbContext : DbContext, IDataProtectionKeyContext
{
    /// <summary>
    /// Tenant context for multi-tenant data isolation.
    /// Set via property injection after DbContext is retrieved from pool.
    /// When null, Global Query Filters are bypassed (e.g., during migrations).
    /// </summary>
    public ITenantContext? TenantContext { get; set; }

    /// <summary>
    /// Current user service for audit field population.
    /// Set via property injection after DbContext is retrieved from pool.
    /// When null (e.g., during migrations), audit fields use null values.
    /// </summary>
    public ICurrentUserService? CurrentUserService { get; set; }

    /// <summary>
    /// ASP.NET Core Data Protection key ring, persisted in the primary database.
    /// Keys are used to Protect/Unprotect inline-encrypted secret bindings
    /// and any other Data Protection purposes (anti-forgery, auth cookies).
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public ExploreDbContext(DbContextOptions<ExploreDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExploreDbContext).Assembly);
        ApplyGlobalQueryFilters(modelBuilder);
    }
}
