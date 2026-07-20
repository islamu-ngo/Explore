// ABOUTME: Defines the startup-only storage mode for the platform privacy-erasure ledger.
// ABOUTME: Defaults locally without reading authority secrets and validates explicit retained connections.

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Explore.Application.Configuration;

public enum PrivacyErasureDurabilityMode
{
    ApplicationDatabase,
    RetainedAuthority
}

public sealed class PrivacyErasureDurabilityOptions
{
    public const string SectionName = "PrivacyErasure:Durability";
    public const string ConnectionStringName = "PrivacyErasureAuthority";

    public PrivacyErasureDurabilityMode Mode { get; set; } =
        PrivacyErasureDurabilityMode.ApplicationDatabase;

    public static PrivacyErasureDurabilityOptions FromConfiguration(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string? configuredMode = configuration[$"{SectionName}:Mode"];
        PrivacyErasureDurabilityMode mode;
        if (configuredMode is null
            || string.Equals(
                configuredMode,
                nameof(PrivacyErasureDurabilityMode.ApplicationDatabase),
                StringComparison.OrdinalIgnoreCase))
        {
            mode = PrivacyErasureDurabilityMode.ApplicationDatabase;
        }
        else if (string.Equals(
            configuredMode,
            nameof(PrivacyErasureDurabilityMode.RetainedAuthority),
            StringComparison.OrdinalIgnoreCase))
        {
            mode = PrivacyErasureDurabilityMode.RetainedAuthority;
        }
        else
        {
            throw new OptionsValidationException(
                nameof(PrivacyErasureDurabilityOptions),
                typeof(PrivacyErasureDurabilityOptions),
                [$"{SectionName}:Mode must be ApplicationDatabase or RetainedAuthority."]);
        }

        if (mode == PrivacyErasureDurabilityMode.RetainedAuthority)
        {
            _ = GetRetainedAuthorityConnectionString(configuration);
        }

        return new PrivacyErasureDurabilityOptions
        {
            Mode = mode
        };
    }

    public static string GetRetainedAuthorityConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString) || !HasRequiredNpgsqlShape(connectionString))
        {
            throw new OptionsValidationException(
                nameof(PrivacyErasureDurabilityOptions),
                typeof(PrivacyErasureDurabilityOptions),
                [$"ConnectionStrings:{ConnectionStringName} must be a valid Npgsql Host/Database/Username connection string in RetainedAuthority mode."]);
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
}
