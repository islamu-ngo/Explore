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

    public PrivacyErasureAuthorityTopology Topology { get; set; } =
        PrivacyErasureAuthorityTopology.CoLocated;

    public static PrivacyErasureDurabilityOptions FromConfiguration(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (HasLegacyModeKey(configuration))
        {
            throw InvalidConfiguration(
                $"{LegacyModeKey} is no longer supported. Remove it, reset this pre-v1 development deployment, and configure {SectionName}:Topology as CoLocated or ExternalDatabase.");
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

        if (topology == PrivacyErasureAuthorityTopology.ExternalDatabase)
        {
            _ = GetExternalDatabaseConnectionString(configuration);
        }

        return new PrivacyErasureDurabilityOptions
        {
            Topology = topology
        };
    }

    public static string GetExternalDatabaseConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString) || !HasRequiredNpgsqlShape(connectionString))
        {
            throw InvalidConfiguration(
                $"ConnectionStrings:{ConnectionStringName} must be a valid Npgsql Host/Database/Username connection string when {SectionName}:Topology is ExternalDatabase.");
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
