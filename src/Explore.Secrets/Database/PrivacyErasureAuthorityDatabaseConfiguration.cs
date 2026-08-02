// ABOUTME: Binds the external privacy-erasure authority from structured PostgreSQL settings.
// ABOUTME: Reuses primary database validation and native Npgsql construction without raw-string inputs.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Explore.Secrets.Database;

public static class PrivacyErasureAuthorityDatabaseConfiguration
{
    public const string SectionName = "PrivacyErasureAuthorityDatabase";
    public const string MigrationsHistoryTable = "__EFPrivacyErasureAuthorityMigrationsHistory";

    private const string EnvironmentPrefix = "PRIVACY_ERASURE_AUTHORITY_";

    public static PrimaryDatabaseConnectionOptions BindRuntime(IConfiguration configuration) =>
        Bind(configuration, PrimaryDatabaseRole.Runtime);

    public static PrimaryDatabaseConnectionOptions BindMigrator(IConfiguration configuration) =>
        Bind(configuration, PrimaryDatabaseRole.Migrator);

    public static PrimaryDatabaseConnectionOptions Bind(
        IConfiguration configuration,
        PrimaryDatabaseRole role)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfiguration projected = BuildPrimaryProjection(configuration);
        PrimaryDatabaseConnectionOptions options;
        try
        {
            options = PrimaryDatabaseConfiguration.Bind(projected, role);
        }
        catch (Exception exception) when (exception is InvalidOperationException or OptionsValidationException)
        {
            throw InvalidConfiguration(exception.Message);
        }

        if (options.Provider != PrimaryDatabaseProvider.PostgreSql)
        {
            throw InvalidConfiguration($"{SectionName}:Provider must be PostgreSql.");
        }

        return options;
    }

    public static PrimaryDatabaseConnectionResult ResolveRuntimeConnectionString(
        IConfiguration configuration) =>
        PrimaryDatabaseConfiguration.BuildConnectionString(BindRuntime(configuration));

    public static PrimaryDatabaseConnectionResult ResolveMigratorConnectionString(
        IConfiguration configuration) =>
        PrimaryDatabaseConfiguration.BuildConnectionString(BindMigrator(configuration));

    public static void ProjectDiscreteConfiguration(IConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        IConfiguration configuration = configurationBuilder.Build();
        Dictionary<string, string?> projected = DiscreteProjection(configuration);
        if (projected.Count > 0)
        {
            configurationBuilder.AddInMemoryCollection(projected);
        }
    }

    private static IConfiguration BuildPrimaryProjection(IConfiguration configuration)
    {
        var values = new Dictionary<string, string?>
        {
            ["Database:Provider"] = Read(configuration, "Provider", discreteSuffix: null),
            ["Database:Host"] = Read(configuration, "Host", "HOST"),
            ["Database:Port"] = Read(configuration, "Port", "PORT"),
            ["Database:Database"] = Read(configuration, "Database", "DATABASE"),
            ["Database:TlsMode"] = Read(configuration, "TlsMode", "TLS_MODE"),
            ["Database:TrustServerCertificate"] = Read(
                configuration,
                "TrustServerCertificate",
                "TRUST_SERVER_CERTIFICATE"),
            ["Database:Runtime:Username"] = ReadRole(configuration, "Runtime", "Username", "RUNTIME_USERNAME"),
            ["Database:Runtime:Password"] = ReadRole(configuration, "Runtime", "Password", "RUNTIME_PASSWORD"),
            ["Database:Migrator:Username"] = ReadRole(configuration, "Migrator", "Username", "MIGRATOR_USERNAME"),
            ["Database:Migrator:Password"] = ReadRole(configuration, "Migrator", "Password", "MIGRATOR_PASSWORD"),
        };

        if (string.IsNullOrWhiteSpace(values["Database:Provider"])
            && values.Any(pair => !string.IsNullOrWhiteSpace(pair.Value)))
        {
            values["Database:Provider"] = nameof(PrimaryDatabaseProvider.PostgreSql);
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static Dictionary<string, string?> DiscreteProjection(IConfiguration configuration)
    {
        var projected = new Dictionary<string, string?>();
        Project(projected, configuration, "Host", "HOST");
        Project(projected, configuration, "Port", "PORT");
        Project(projected, configuration, "Database", "DATABASE");
        Project(projected, configuration, "TlsMode", "TLS_MODE");
        Project(projected, configuration, "TrustServerCertificate", "TRUST_SERVER_CERTIFICATE");
        ProjectRole(projected, configuration, "Runtime", "Username", "RUNTIME_USERNAME");
        ProjectRole(projected, configuration, "Runtime", "Password", "RUNTIME_PASSWORD");
        ProjectRole(projected, configuration, "Migrator", "Username", "MIGRATOR_USERNAME");
        ProjectRole(projected, configuration, "Migrator", "Password", "MIGRATOR_PASSWORD");

        if (projected.Count > 0 && string.IsNullOrWhiteSpace(configuration[$"{SectionName}:Provider"]))
        {
            projected[$"{SectionName}:Provider"] = nameof(PrimaryDatabaseProvider.PostgreSql);
        }

        return projected;
    }

    private static string? Read(
        IConfiguration configuration,
        string field,
        string? discreteSuffix,
        string? defaultValue = null)
    {
        string? explicitValue = configuration[$"{SectionName}:{field}"];
        if (!string.IsNullOrWhiteSpace(explicitValue))
        {
            return explicitValue;
        }

        return discreteSuffix is null
            ? defaultValue
            : configuration[$"{EnvironmentPrefix}{discreteSuffix}"] ?? defaultValue;
    }

    private static string? ReadRole(
        IConfiguration configuration,
        string role,
        string field,
        string discreteSuffix)
    {
        string? explicitValue = configuration[$"{SectionName}:{role}:{field}"];
        return !string.IsNullOrWhiteSpace(explicitValue)
            ? explicitValue
            : configuration[$"{EnvironmentPrefix}{discreteSuffix}"];
    }

    private static void Project(
        IDictionary<string, string?> projected,
        IConfiguration configuration,
        string field,
        string discreteSuffix)
    {
        string target = $"{SectionName}:{field}";
        string? value = configuration[$"{EnvironmentPrefix}{discreteSuffix}"];
        if (string.IsNullOrWhiteSpace(configuration[target]) && !string.IsNullOrWhiteSpace(value))
        {
            projected[target] = value;
        }
    }

    private static void ProjectRole(
        IDictionary<string, string?> projected,
        IConfiguration configuration,
        string role,
        string field,
        string discreteSuffix)
    {
        string target = $"{SectionName}:{role}:{field}";
        string? value = configuration[$"{EnvironmentPrefix}{discreteSuffix}"];
        if (string.IsNullOrWhiteSpace(configuration[target]) && !string.IsNullOrWhiteSpace(value))
        {
            projected[target] = value;
        }
    }

    private static OptionsValidationException InvalidConfiguration(string failure) =>
        new(SectionName, typeof(PrimaryDatabaseConnectionOptions), [failure]);
}
