// ABOUTME: Loads Postgres bootstrap credentials (Host/Port/Database/Username/Password)
// ABOUTME: from Infisical -> environment variables -> IConfiguration, in that strict order.

using System.Globalization;
using Infisical.Sdk;
using Infisical.Sdk.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Explore.Secrets.Bootstrap;

/// <summary>
/// Resolves the Postgres connection string during application bootstrap, before
/// <c>ExploreDbContext</c> (and therefore <c>ISecretResolver</c>) is available.
/// </summary>
/// <remarks>
/// <para>
/// This loader deliberately does NOT use <c>ISecretResolver</c> or <c>SecretBinding</c> rows:
/// the DB that holds those bindings cannot unlock its own connection-string secrets
/// (chicken-and-egg). The <c>SecretDefinitionRegistry</c> enforces the matching
/// invariant at the domain layer by marking <c>postgresql.*</c> keys as
/// <c>IsBootstrapSecret = true</c> and disallowing <c>InlineEncrypted</c>.
/// </para>
/// <para>
/// Resolution order (first non-empty match wins, per-field, highest priority first):
/// </para>
/// <list type="number">
///   <item><description>Infisical: when <c>SecretProvider:Infisical:ClientId</c>/<c>ClientSecret</c>
///     are supplied, the <c>/postgresql</c> folder is fetched directly via <see cref="InfisicalClient"/>
///     (no caching layer, one-shot, synchronous-adjacent) and fields are pulled from the
///     <c>POSTGRESQL_HOST/PORT/DATABASE/USERNAME/PASSWORD</c> secrets defined by the user.</description></item>
///   <item><description>Environment variables: <c>POSTGRESQL_HOST</c>, <c>POSTGRESQL_PORT</c>,
///     <c>POSTGRESQL_DATABASE</c>, <c>POSTGRESQL_USERNAME</c>, <c>POSTGRESQL_PASSWORD</c>.</description></item>
///   <item><description>IConfiguration section <c>Postgresql:*</c> (Host/Port/Database/Username/Password)
///     fed by <c>appsettings.json</c>, user secrets, or command-line args.</description></item>
/// </list>
/// <para>
/// There is no <c>POSTGRESQL_PUBLIC_URL</c> fallback. The URL form is deliberately removed
/// so the connection string is always constructed by <see cref="NpgsqlConnectionStringBuilder"/>
/// with <c>SSL Mode = Prefer</c> and <c>Trust Server Certificate = true</c> (safe defaults for
/// self-hosted operators who may or may not front Postgres with a TLS terminator).
/// </para>
/// <para>
/// If any required field is missing after all three sources have been consulted, a single
/// <see cref="InvalidOperationException"/> is thrown with an actionable message listing
/// exactly which fields are missing. Startup fails loudly rather than silently producing
/// a broken connection string.
/// </para>
/// </remarks>
public static class BootstrapSecretLoader
{
    /// <summary>Canonical Infisical folder path for Postgres secrets.</summary>
    public const string InfisicalPath = "/postgresql";

    /// <summary>Expected Infisical secret key for the Postgres host.</summary>
    public const string InfisicalKeyHost = "POSTGRESQL_HOST";

    /// <summary>Expected Infisical secret key for the Postgres port.</summary>
    public const string InfisicalKeyPort = "POSTGRESQL_PORT";

    /// <summary>Expected Infisical secret key for the Postgres database name.</summary>
    public const string InfisicalKeyDatabase = "POSTGRESQL_DATABASE";

    /// <summary>Expected Infisical secret key for the Postgres username.</summary>
    public const string InfisicalKeyUsername = "POSTGRESQL_USERNAME";

    /// <summary>Expected Infisical secret key for the Postgres password.</summary>
    public const string InfisicalKeyPassword = "POSTGRESQL_PASSWORD";

    /// <summary>Environment variable for the Postgres host.</summary>
    public const string EnvHost = "POSTGRESQL_HOST";

    /// <summary>Environment variable for the Postgres port.</summary>
    public const string EnvPort = "POSTGRESQL_PORT";

    /// <summary>Environment variable for the Postgres database name.</summary>
    public const string EnvDatabase = "POSTGRESQL_DATABASE";

    /// <summary>Environment variable for the Postgres username.</summary>
    public const string EnvUsername = "POSTGRESQL_USERNAME";

    /// <summary>Environment variable for the Postgres password.</summary>
    public const string EnvPassword = "POSTGRESQL_PASSWORD";

    /// <summary>IConfiguration key for the Postgres host.</summary>
    public const string ConfigHost = "Postgresql:Host";

    /// <summary>IConfiguration key for the Postgres port.</summary>
    public const string ConfigPort = "Postgresql:Port";

    /// <summary>IConfiguration key for the Postgres database name.</summary>
    public const string ConfigDatabase = "Postgresql:Database";

    /// <summary>IConfiguration key for the Postgres username.</summary>
    public const string ConfigUsername = "Postgresql:Username";

    /// <summary>IConfiguration key for the Postgres password.</summary>
    public const string ConfigPassword = "Postgresql:Password";

    /// <summary>Default Postgres port when not supplied anywhere.</summary>
    public const int DefaultPort = 5432;

    /// <summary>
    /// Resolves Postgres bootstrap credentials and returns a composed Npgsql connection string.
    /// </summary>
    /// <param name="configuration">Bootstrap <see cref="IConfiguration"/> (env vars + appsettings).
    /// Used both to discover Infisical bootstrap credentials and as the lowest-priority fallback
    /// for the Postgres fields themselves.</param>
    /// <param name="logger">Optional logger; loader logs the winning source per field without
    /// ever logging the password or host values.</param>
    /// <returns>A <see cref="BootstrapPostgresCredentials"/> with the composed connection string
    /// and a label describing the winning source.</returns>
    /// <exception cref="InvalidOperationException">One or more required fields could not be
    /// resolved from any source. The exception message lists the missing field names.</exception>
    public static BootstrapPostgresCredentials LoadPostgresConnectionString(
        IConfiguration configuration,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Infisical is optional. If bootstrap Infisical creds are present we fetch the
        // /postgresql folder once, synchronously, and use those values as the top priority.
        // Any Infisical failure is logged but non-fatal: env vars or config can still win.
        var infisicalSecrets = TryLoadInfisicalPostgresFolder(configuration, logger);

        var (host, hostSource) = ResolveField(
            infisicalSecrets, InfisicalKeyHost, EnvHost, ConfigHost, configuration);
        var (portRaw, portSource) = ResolveField(
            infisicalSecrets, InfisicalKeyPort, EnvPort, ConfigPort, configuration);
        var (database, databaseSource) = ResolveField(
            infisicalSecrets, InfisicalKeyDatabase, EnvDatabase, ConfigDatabase, configuration);
        var (username, usernameSource) = ResolveField(
            infisicalSecrets, InfisicalKeyUsername, EnvUsername, ConfigUsername, configuration);
        var (password, passwordSource) = ResolveField(
            infisicalSecrets, InfisicalKeyPassword, EnvPassword, ConfigPassword, configuration);

        var missing = new List<string>(5);
        if (string.IsNullOrWhiteSpace(host)) missing.Add(nameof(host));
        if (string.IsNullOrWhiteSpace(database)) missing.Add(nameof(database));
        if (string.IsNullOrWhiteSpace(username)) missing.Add(nameof(username));
        if (string.IsNullOrWhiteSpace(password)) missing.Add(nameof(password));

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Postgres bootstrap credentials are incomplete. Missing required fields: "
                + string.Join(", ", missing)
                + ". Provide them via Infisical (/postgresql folder: "
                + $"{InfisicalKeyHost}/{InfisicalKeyDatabase}/{InfisicalKeyUsername}/{InfisicalKeyPassword}), "
                + "environment variables "
                + $"({EnvHost}/{EnvDatabase}/{EnvUsername}/{EnvPassword}), "
                + "or appsettings.json section 'Postgresql:{Host,Database,Username,Password}'.");
        }

        var port = ParsePort(portRaw, logger);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host!,
            Port = port,
            Database = database!,
            Username = username!,
            Password = password!,
            SslMode = SslMode.Prefer,
            TrustServerCertificate = true,
        };

        var winningSource = DescribeWinningSource(
            hostSource, portSource, databaseSource, usernameSource, passwordSource);

        logger?.LogInformation(
            "Bootstrap Postgres credentials resolved from {Source} (host via {HostSource}, "
            + "port via {PortSource}, database via {DatabaseSource}, username via {UsernameSource}, "
            + "password via {PasswordSource}).",
            winningSource,
            hostSource,
            portSource,
            databaseSource,
            usernameSource,
            passwordSource);

        return new BootstrapPostgresCredentials(
            builder.ConnectionString,
            winningSource,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Parses a Postgres port from the resolved raw string, falling back to 5432 and
    /// logging a warning if the value is unparseable (we prefer "work with default"
    /// over "hard-fail on malformed port" for self-hosted operators).
    /// </summary>
    private static int ParsePort(string? portRaw, ILogger? logger)
    {
        if (string.IsNullOrWhiteSpace(portRaw))
        {
            return DefaultPort;
        }

        if (int.TryParse(portRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
            && port is > 0 and < 65536)
        {
            return port;
        }

        logger?.LogWarning(
            "Postgres bootstrap port value is not a valid TCP port. Falling back to {DefaultPort}.",
            DefaultPort);
        return DefaultPort;
    }

    /// <summary>
    /// Resolves a single field from the priority chain Infisical -> env var -> IConfiguration.
    /// Returns the value plus a human-readable label describing which source won.
    /// </summary>
    private static (string? Value, string Source) ResolveField(
        IReadOnlyDictionary<string, string>? infisicalSecrets,
        string infisicalKey,
        string envKey,
        string configKey,
        IConfiguration configuration)
    {
        if (infisicalSecrets is not null
            && infisicalSecrets.TryGetValue(infisicalKey, out var infisicalValue)
            && !string.IsNullOrWhiteSpace(infisicalValue))
        {
            return (infisicalValue, $"Infisical:{InfisicalPath}/{infisicalKey}");
        }

        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return (envValue, $"EnvironmentVariable:{envKey}");
        }

        var configValue = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(configValue))
        {
            return (configValue, $"IConfiguration:{configKey}");
        }

        return (null, "<unresolved>");
    }

    /// <summary>
    /// Produces a high-level summary of which source family supplied most fields.
    /// Matches the priority chain: Infisical > EnvironmentVariables > IConfiguration > Mixed.
    /// </summary>
    private static string DescribeWinningSource(params string[] fieldSources)
    {
        var distinctPrefixes = fieldSources
            .Select(static s => s.Split(':', 2)[0])
            .Where(static p => !string.Equals(p, "<unresolved>", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return distinctPrefixes.Count switch
        {
            0 => "<unresolved>",
            1 => distinctPrefixes[0],
            _ => $"Mixed({string.Join("+", distinctPrefixes)})",
        };
    }

    /// <summary>
    /// Best-effort fetch of the /postgresql Infisical folder during bootstrap. Returns null
    /// when Infisical bootstrap credentials are absent, or when the SDK call fails (we log
    /// and continue so env/config can still satisfy the chain).
    /// </summary>
    private static IReadOnlyDictionary<string, string>? TryLoadInfisicalPostgresFolder(
        IConfiguration configuration,
        ILogger? logger)
    {
        var section = configuration.GetSection($"{Configuration.SecretProviderOptions.SectionName}:Infisical");
        var projectId = section["ProjectId"];
        var clientId = section["ClientId"];
        var clientSecret = section["ClientSecret"];
        var environment = section["Environment"] ?? "dev";
        var url = section["Url"];

        if (string.IsNullOrWhiteSpace(projectId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret))
        {
            logger?.LogDebug(
                "Infisical bootstrap credentials not provided; skipping Infisical for Postgres bootstrap.");
            return null;
        }

        try
        {
            var settings = new InfisicalSdkSettingsBuilder()
                .WithHostUri(url ?? "https://app.infisical.com")
                .Build();

            var client = new InfisicalClient(settings);
            try
            {
                client.Auth().UniversalAuth().LoginAsync(clientId, clientSecret).GetAwaiter().GetResult();

                var options = new ListSecretsOptions
                {
                    ProjectId = projectId,
                    EnvironmentSlug = environment,
                    SecretPath = InfisicalPath,
                    Recursive = false,
                    ExpandSecretReferences = true,
                    ViewSecretValue = true,
                };

                var secrets = client.Secrets().ListAsync(options).GetAwaiter().GetResult();
                if (secrets is null)
                {
                    logger?.LogWarning(
                        "Infisical returned no secrets for path {Path}.", InfisicalPath);
                    return null;
                }

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var secret in secrets)
                {
                    dict[secret.SecretKey] = secret.SecretValue;
                }

                logger?.LogInformation(
                    "Infisical bootstrap loaded {Count} secrets from {Path}.",
                    dict.Count,
                    InfisicalPath);
                return dict;
            }
            finally
            {
                // InfisicalClient may implement IDisposable or IAsyncDisposable depending
                // on SDK version; dispose defensively so the client connection is released.
                if (client is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (client is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "Infisical bootstrap failed; falling back to environment variables and IConfiguration.");
            return null;
        }
    }
}
