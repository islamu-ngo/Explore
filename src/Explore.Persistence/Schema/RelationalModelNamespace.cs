// ABOUTME: Applies the fixed relational namespace to the shared Explore EF Core model.
// ABOUTME: Uses schemas where supported and a deterministic table prefix otherwise.

using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Schema;

internal static class RelationalModelNamespace
{
    internal const string Name = "islamu_event";
    internal const string Prefix = Name + "_";

    public static void Apply(ModelBuilder modelBuilder, string? providerName)
    {
        if (SupportsSchemas(providerName))
        {
            modelBuilder.HasDefaultSchema(Name);
            return;
        }

        if (providerName is not ("Microsoft.EntityFrameworkCore.Sqlite" or "Microting.EntityFrameworkCore.MySql"))
        {
            return;
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName) && !tableName.StartsWith(Prefix, StringComparison.Ordinal))
            {
                entityType.SetTableName(Prefix + tableName);
            }
        }
    }

    private static bool SupportsSchemas(string? providerName) =>
        providerName is "Npgsql.EntityFrameworkCore.PostgreSQL" or "Microsoft.EntityFrameworkCore.SqlServer";
}
