// ABOUTME: Normalizes PostgreSQL-oriented relational annotations for the other supported database providers.
// ABOUTME: Preserves PostgreSQL types while emitting portable defaults and constraint SQL for every provider.

using System.Text.RegularExpressions;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Explore.Persistence.Schema;

internal static partial class PortableRelationalModelPolicy
{
    private const string PostgreSqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";
    private const string MySqlProvider = "Microting.EntityFrameworkCore.MySql";
    private const int MySqlLookupIndexPrefixLength = 512;

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
        if (providerName is null)
        {
            return;
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            NormalizeProperties(entityType, providerName);
            if (providerName == SqlServerProvider)
            {
                NormalizeSqlServerDeleteBehaviors(entityType);
            }
            if (providerName == PostgreSqlProvider)
            {
                continue;
            }

            NormalizeCheckConstraints(entityType, providerName);
            NormalizeIndexFilters(entityType, providerName);
        }
    }

    private static void NormalizeProperties(IMutableEntityType entityType, string providerName)
    {
        foreach (var property in entityType.GetProperties())
        {
            if (providerName == SqlServerProvider &&
                property.ClrType == typeof(int) &&
                property.IsPrimaryKey())
            {
                property.ValueGenerated = ValueGenerated.Never;
            }

            if (providerName != PostgreSqlProvider && property.GetCollation() is not null)
            {
                property.SetCollation(null);
            }

            if (providerName != PostgreSqlProvider &&
                PostgreSqlColumnTypes.Contains(property.GetColumnType() ?? string.Empty))
            {
                property.SetColumnType(null);
            }

            if (providerName == SqlServerProvider &&
                string.Equals(property.GetColumnType(), "text", StringComparison.OrdinalIgnoreCase))
            {
                property.SetColumnType(null);
            }

            var computedSql = property.GetComputedColumnSql();
            if (providerName == MySqlProvider &&
                computedSql?.Contains("COALESCE(", StringComparison.OrdinalIgnoreCase) == true)
            {
                property.SetComputedColumnSql(null);
                property.ValueGenerated = ValueGenerated.Never;
                property.SetBeforeSaveBehavior(PropertySaveBehavior.Save);
                computedSql = null;
            }

            if (providerName != PostgreSqlProvider && computedSql is not null)
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
                property.SetValueGeneratorFactory((_, _) => new GuidVersion7ValueGenerator());
                continue;
            }

            if (providerName != PostgreSqlProvider)
            {
                property.SetDefaultValueSql(NormalizeTimestampDefault(defaultSql, providerName));
            }
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
                MySqlProvider => $"(TIMESTAMPADD(DAY, {days}, UTC_TIMESTAMP()))",
                _ => $"datetime('now', '+{days} days')"
            };
        }

        if (sql.Equals("'infinity'::timestamp with time zone", StringComparison.OrdinalIgnoreCase))
        {
            return providerName switch
            {
                SqlServerProvider => "CONVERT(datetimeoffset, '9999-12-31T23:59:59.9999999+00:00', 127)",
                MySqlProvider => "(CAST('9999-12-31 23:59:59.999999' AS DATETIME(6)))",
                _ => "'9999-12-31 23:59:59.9999999+00:00'"
            };
        }

        if (sql.Equals("NOW()", StringComparison.OrdinalIgnoreCase) ||
            sql.Equals("statement_timestamp()", StringComparison.OrdinalIgnoreCase))
        {
            return providerName switch
            {
                SqlServerProvider => "SYSUTCDATETIME()",
                MySqlProvider => "(UTC_TIMESTAMP())",
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

            if (providerName == MySqlProvider)
            {
                sql = IsNotDistinctFrom().Replace(sql, "${left} <=> ${right}");
            }

            if (UnsupportedCheckSqlTokens.Any(token => sql.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                entityType.RemoveCheckConstraint(constraintName);
                continue;
            }

            if (providerName == SqlServerProvider)
            {
                sql = sql.Replace("length(", "len(", StringComparison.OrdinalIgnoreCase);
                sql = NormalizeSqlServerBooleanPredicates(entityType, sql);

                var equivalence = PredicateEquivalence().Match(sql);
                if (equivalence.Success)
                {
                    sql = $"(CASE WHEN {equivalence.Groups["left"].Value} THEN 1 ELSE 0 END) = " +
                          $"(CASE WHEN {equivalence.Groups["right"].Value} THEN 1 ELSE 0 END)";
                }
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
            if (providerName == MySqlProvider && !index.IsUnique)
            {
                var prefixLengths = index.Properties
                    .Select(property => property.ClrType == typeof(string) &&
                                        property.GetMaxLength() is > MySqlLookupIndexPrefixLength
                        ? MySqlLookupIndexPrefixLength
                        : 0)
                    .ToArray();
                if (prefixLengths.Any(length => length > 0))
                {
                    index.SetPrefixLength(prefixLengths);
                }
            }

            var filter = index.GetFilter();
            if (filter is not null)
            {
                index.SetFilter(NormalizeBooleanLiterals(filter, providerName));
            }
        }
    }

    private static void NormalizeSqlServerDeleteBehaviors(IMutableEntityType entityType)
    {
        foreach (var foreignKey in entityType.GetForeignKeys()
                     .Where(foreignKey => foreignKey.DeleteBehavior is
                         DeleteBehavior.Cascade or DeleteBehavior.SetNull))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }

    private static string NormalizeBooleanLiterals(string sql, string providerName) =>
        providerName == SqlServerProvider
            ? TrueLiteral().Replace(FalseLiteral().Replace(sql, "0"), "1")
            : sql;

    private static string NormalizeSqlServerBooleanPredicates(
        IMutableEntityType entityType,
        string sql)
    {
        foreach (var property in entityType.GetProperties()
                     .Where(property => property.ClrType == typeof(bool)))
        {
            var columnName = property.GetColumnName();
            if (string.IsNullOrWhiteSpace(columnName))
            {
                continue;
            }

            var escapedColumn = Regex.Escape(columnName);
            sql = Regex.Replace(
                sql,
                $@"\bNOT\s+{escapedColumn}\b",
                $"{columnName} = 0",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            sql = Regex.Replace(
                sql,
                $@"\b{escapedColumn}\b(?=\s+(?:AND|OR)|\s*\))",
                $"{columnName} = 1",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return sql;
    }

    [GeneratedRegex(@"length\(btrim\((?<column>[a-z0-9_]+)\)\)\s*>\s*0", RegexOptions.IgnoreCase)]
    private static partial Regex NonBlankBtrim();

    [GeneratedRegex(@"^statement_timestamp\(\)\s*\+\s*INTERVAL\s*'(\d+) days'$", RegexOptions.IgnoreCase)]
    private static partial Regex PostgreSqlDayInterval();

    [GeneratedRegex(@"\bfalse\b", RegexOptions.IgnoreCase)]
    private static partial Regex FalseLiteral();

    [GeneratedRegex(@"\btrue\b", RegexOptions.IgnoreCase)]
    private static partial Regex TrueLiteral();

    [GeneratedRegex(@"^\((?<left>.+)\)\s*=\s*\((?<right>.+)\)$", RegexOptions.Singleline)]
    private static partial Regex PredicateEquivalence();

    [GeneratedRegex(@"(?<left>[a-z0-9_]+)\s+IS\s+NOT\s+DISTINCT\s+FROM\s+(?<right>[a-z0-9_]+)", RegexOptions.IgnoreCase)]
    private static partial Regex IsNotDistinctFrom();
}
