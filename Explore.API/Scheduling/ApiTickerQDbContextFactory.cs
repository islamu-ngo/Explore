// ABOUTME: Design-time factory for generating TickerQ operational-store EF migrations.
// ABOUTME: Avoids bootstrapping the full API host or external providers during dotnet-ef operations.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Explore.API.Scheduling;

public sealed class ApiTickerQDbContextFactory : IDesignTimeDbContextFactory<ApiTickerQDbContext>
{
    public ApiTickerQDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApiTickerQDbContext>()
            .UseNpgsql("Host=localhost;Database=tickerq_design_time;Username=postgres;Password=postgres")
            .Options;

        return new ApiTickerQDbContext(options);
    }
}
