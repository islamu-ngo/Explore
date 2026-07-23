// ABOUTME: Defines the startup-only topology for the platform privacy-erasure authority.
// ABOUTME: Defaults to CoLocated and reads an external authority connection only when explicitly selected.

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Explore.Application.Configuration;

public enum PrivacyErasureAuthorityTopology
{
    CoLocated,
    ExternalDatabase
}

public sealed class PrivacyErasureDurabilityOptions
{
    public const string SectionName = "PrivacyErasure:Authority";
    public const string LegacyModeKey = "PrivacyErasure:Durability:Mode";
    public const string ConnectionStringName = "PrivacyErasureAuthority";
    public const string MigratorConnectionStringName = "PrivacyErasureAuthorityMigrator";

    public PrivacyErasureAuthorityTopology Topology { get; set; } =
        PrivacyErasureAuthorityTopology.CoLocated;

    public bool RestoreReplayProtection =>
        Topology == PrivacyErasureAuthorityTopology.ExternalDatabase;

    public static PrivacyErasureDurabilityOptions FromConfiguration(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        PrivacyErasureAuthorityTopology topology = GetTopology(configuration);
        if (topology == PrivacyErasureAuthorityTopology.ExternalDatabase)
        {
            _ = GetExternalDatabaseConnectionString(configuration);
        }

        return new PrivacyErasureDurabilityOptions
        {
            Topology = topology
        };
    }

    public static PrivacyErasureAuthorityTopology GetTopology(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (HasLegacyModeKey(configuration))
        {
            throw InvalidConfiguration(
                $"{LegacyModeKey} is no longer supported. Confirm this pre-v1 development deployment is reset-eligible, create and verify a backup or export for every value the operator must retain, then perform an operator-managed reset, remove the legacy key, and configure {SectionName}:Topology as CoLocated or ExternalDatabase.");
        }

        string? configuredTopology = configuration[$"{SectionName}:Topology"];
        PrivacyErasureAuthorityTopology topology;
        if (configuredTopology is null
            || string.Equals(
                configuredTopology,
                nameof(PrivacyErasureAuthorityTopology.CoLocated),
                StringComparison.OrdinalIgnoreCase))
        {
            topology = PrivacyErasureAuthorityTopology.CoLocated;
        }
        else if (string.Equals(
            configuredTopology,
            nameof(PrivacyErasureAuthorityTopology.ExternalDatabase),
            StringComparison.OrdinalIgnoreCase))
        {
            topology = PrivacyErasureAuthorityTopology.ExternalDatabase;
        }
        else
        {
            throw InvalidConfiguration(
                $"{SectionName}:Topology must be CoLocated or ExternalDatabase.");
        }

        return topology;
    }

    public static string GetExternalDatabaseConnectionString(IConfiguration configuration)
        => GetConnectionString(configuration, ConnectionStringName);

    public static string GetExternalDatabaseMigratorConnectionString(IConfiguration configuration)
        => GetConnectionString(configuration, MigratorConnectionStringName);

    private static string GetConnectionString(IConfiguration configuration, string name)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string? connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString) || !HasRequiredNpgsqlShape(connectionString))
        {
            throw InvalidConfiguration(
                $"ConnectionStrings:{name} must be a valid Npgsql Host/Database/Username connection string when {SectionName}:Topology is ExternalDatabase.");
        }

        return connectionString;
    }

    private static bool HasRequiredNpgsqlShape(string connectionString)
    {
        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
            return HasNonBlankValue(builder, "Host")
                && HasNonBlankValue(builder, "Database")
                && (HasNonBlankValue(builder, "Username")
                    || HasNonBlankValue(builder, "User ID"));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool HasNonBlankValue(DbConnectionStringBuilder builder, string key) =>
        builder.TryGetValue(key, out object? value)
        && !string.IsNullOrWhiteSpace(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));

    private static bool HasLegacyModeKey(IConfiguration configuration) =>
        configuration[LegacyModeKey] is not null
        || configuration.GetSection("PrivacyErasure:Durability")
            .GetChildren()
            .Any(section => section.Key.Equals("Mode", StringComparison.OrdinalIgnoreCase));

    private static OptionsValidationException InvalidConfiguration(string failure) =>
        new(
            nameof(PrivacyErasureDurabilityOptions),
            typeof(PrivacyErasureDurabilityOptions),
            [failure]);
}
