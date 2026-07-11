// ABOUTME: Concrete EF Core context for API-hosted TickerQ operational tables.
// ABOUTME: Binds TickerQ's generic time and cron entities so scheduler persistence has a real model.

using Microsoft.EntityFrameworkCore;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.Utilities.Entities;

namespace Explore.API.Scheduling;

public sealed class ApiTickerQDbContext(DbContextOptions<ApiTickerQDbContext> options)
    : TickerQDbContext<TimeTickerEntity, CronTickerEntity>(options)
{
    public const string Schema = "ticker";
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder);
    }
}
