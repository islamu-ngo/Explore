// ABOUTME: Dedicated EF Core context for the ASP.NET Core Data Protection key ring.
// ABOUTME: Keeps BFF cookie key persistence isolated from the multi-tenant application DbContext.

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence;

public sealed class DataProtectionKeyContext(DbContextOptions<DataProtectionKeyContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}
