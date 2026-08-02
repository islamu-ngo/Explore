// ABOUTME: Defines the startup-only topology for the platform privacy-erasure authority.
// ABOUTME: Defaults to restore-isolated EmbeddedSqlite with ExternalDatabase as the enterprise option.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Explore.Application.Configuration;

public enum PrivacyErasureAuthorityTopology
{
    EmbeddedSqlite,
    ExternalDatabase
}

public sealed class PrivacyErasureDurabilityOptions
{
    public const string SectionName = "PrivacyErasure:Authority";
    public const string LegacyModeKey = "PrivacyErasure:Durability:Mode";
    public PrivacyErasureAuthorityTopology Topology { get; set; } =
        PrivacyErasureAuthorityTopology.EmbeddedSqlite;

    public bool RestoreReplayProtection => true;

    public static PrivacyErasureDurabilityOptions FromConfiguration(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new PrivacyErasureDurabilityOptions
        {
            Topology = GetTopology(configuration)
        };
    }

    public static PrivacyErasureAuthorityTopology GetTopology(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (HasLegacyModeKey(configuration))
        {
            throw InvalidConfiguration(
                $"{LegacyModeKey} is no longer supported. Confirm this pre-v1 development deployment is reset-eligible, create and verify a backup or export for every value the operator must retain, then perform an operator-managed reset, remove the legacy key, and configure {SectionName}:Topology as EmbeddedSqlite or ExternalDatabase.");
        }

        string? configuredTopology = configuration[$"{SectionName}:Topology"];
        PrivacyErasureAuthorityTopology topology;
        if (configuredTopology is null
            || string.Equals(
                configuredTopology,
                nameof(PrivacyErasureAuthorityTopology.EmbeddedSqlite),
                StringComparison.OrdinalIgnoreCase))
        {
            topology = PrivacyErasureAuthorityTopology.EmbeddedSqlite;
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
                $"{SectionName}:Topology must be EmbeddedSqlite or ExternalDatabase.");
        }

        return topology;
    }

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
