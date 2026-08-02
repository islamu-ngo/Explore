// ABOUTME: Normalizes PostgreSQL-oriented relational annotations for the other supported database providers.
// ABOUTME: Preserves the PostgreSQL model while emitting native types, defaults, and portable constraint SQL elsewhere.

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Explore.Persistence.Schema;

internal static partial class PortableRelationalModelPolicy
{
    private const string PostgreSqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";
    private const string MySqlProvider = "Microting.EntityFrameworkCore.MySql";

    private static readonly HashSet<string> PostgreSqlColumnTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bytea",
        "jsonb",
        "time without time zone",
        "timestamp with time zone",
        "uuid"
    };

    private static readonly string[] UnsupportedCheckSqlTokens =
    {
        "::",
        "jsonb_",
        "num_nonnulls(",
        "octet_length(",
        "extract(",
        "~"
    };

    public static void Apply(ModelBuilder modelBuilder, string? providerName)
    {
        if (providerName is null or PostgreSqlProvider)
        {
            return;
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            NormalizeProperties(entityType, providerName);
            NormalizeCheckConstraints(entityType, providerName);
            NormalizeIndexFilters(entityType, providerName);
        }
    }

    private static void NormalizeProperties(IMutableEntityType entityType, string providerName)
    {
        foreach (var property in entityType.GetProperties())
        {
            if (property.GetCollation() is not null)
            {
                property.SetCollation(null);
            }

            if (PostgreSqlColumnTypes.Contains(property.GetColumnType() ?? string.Empty))
            {
                property.SetColumnType(null);
            }

            var computedSql = property.GetComputedColumnSql();
            if (computedSql is not null)
            {
                property.SetComputedColumnSql(computedSql.Replace("::uuid", string.Empty, StringComparison.OrdinalIgnoreCase));
            }

            var defaultSql = property.GetDefaultValueSql();
            if (defaultSql is null)
            {
                continue;
            }

            if (defaultSql.Equals("uuidv7()", StringComparison.OrdinalIgnoreCase))
            {
                property.SetDefaultValueSql(null);
                continue;
            }

            property.SetDefaultValueSql(NormalizeTimestampDefault(defaultSql, providerName));
        }
    }

    private static string NormalizeTimestampDefault(string sql, string providerName)
    {
        var interval = PostgreSqlDayInterval().Match(sql);
        if (interval.Success)
        {
            var days = interval.Groups[1].Value;
            return providerName switch
            {
                SqlServerProvider => $"DATEADD(day, {days}, SYSUTCDATETIME())",
                MySqlProvider => $"TIMESTAMPADD(DAY, {days}, UTC_TIMESTAMP())",
                _ => $"datetime('now', '+{days} days')"
            };
        }

        if (sql.Equals("'infinity'::timestamp with time zone", StringComparison.OrdinalIgnoreCase))
        {
            return providerName switch
            {
                SqlServerProvider => "CONVERT(datetimeoffset, '9999-12-31T23:59:59.9999999+00:00', 127)",
                MySqlProvider => "CAST('9999-12-31 23:59:59.999999' AS DATETIME(6))",
                _ => "'9999-12-31 23:59:59.9999999+00:00'"
            };
        }

        if (sql.Equals("NOW()", StringComparison.OrdinalIgnoreCase) ||
            sql.Equals("statement_timestamp()", StringComparison.OrdinalIgnoreCase))
        {
            return providerName switch
            {
                SqlServerProvider => "SYSUTCDATETIME()",
                MySqlProvider => "UTC_TIMESTAMP()",
                _ => "CURRENT_TIMESTAMP"
            };
        }

        return sql;
    }

    private static void NormalizeCheckConstraints(IMutableEntityType entityType, string providerName)
    {
        foreach (var constraint in entityType.GetCheckConstraints().ToArray())
        {
            var constraintName = constraint.Name;
            if (constraintName is null)
            {
                continue;
            }

            var sql = NonBlankBtrim().Replace(constraint.Sql, "trim(${column}) <> ''")
                .Replace("btrim(", "trim(", StringComparison.OrdinalIgnoreCase);

            if (UnsupportedCheckSqlTokens.Any(token => sql.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                entityType.RemoveCheckConstraint(constraintName);
                continue;
            }

            var normalizedSql = NormalizeBooleanLiterals(sql, providerName);
            if (!normalizedSql.Equals(constraint.Sql, StringComparison.Ordinal))
            {
                entityType.RemoveCheckConstraint(constraintName);
                entityType.AddCheckConstraint(constraintName, normalizedSql);
            }
        }
    }

    private static void NormalizeIndexFilters(IMutableEntityType entityType, string providerName)
    {
        foreach (var index in entityType.GetIndexes())
        {
            var filter = index.GetFilter();
            if (filter is not null)
            {
                index.SetFilter(NormalizeBooleanLiterals(filter, providerName));
            }
        }
    }

    private static string NormalizeBooleanLiterals(string sql, string providerName) =>
        providerName == SqlServerProvider
            ? TrueLiteral().Replace(FalseLiteral().Replace(sql, "0"), "1")
            : sql;

    [GeneratedRegex(@"length\(btrim\((?<column>[a-z0-9_]+)\)\)\s*>\s*0", RegexOptions.IgnoreCase)]
    private static partial Regex NonBlankBtrim();

    [GeneratedRegex(@"^statement_timestamp\(\)\s*\+\s*INTERVAL\s*'(\d+) days'$", RegexOptions.IgnoreCase)]
    private static partial Regex PostgreSqlDayInterval();

    [GeneratedRegex(@"\bfalse\b", RegexOptions.IgnoreCase)]
    private static partial Regex FalseLiteral();

    [GeneratedRegex(@"\btrue\b", RegexOptions.IgnoreCase)]
    private static partial Regex TrueLiteral();
}
