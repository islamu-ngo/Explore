// ABOUTME: Loads Postgres bootstrap credentials (Host/Port/Database/Username/Password)
// ABOUTME: from Infisical -> environment variables -> IConfiguration, in that strict order.

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using Explore.Secrets.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
///   <item><description>Infisical: when <c>Infisical:ClientId</c>/<c>ClientSecret</c>
///     are supplied (bare keys, the canonical repo-wide convention; the legacy
///     <c>SecretProvider:Infisical:*</c> prefix is still accepted as a fallback),
///     the <c>/postgresql</c> folder is fetched directly via <see cref="InfisicalClient"/>
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

    public static void ProjectPostgresConfiguration(
        IConfigurationBuilder configBuilder,
        PrimaryDatabaseRole role,
        bool infisicalAlreadyLoaded = false)
    {
        ArgumentNullException.ThrowIfNull(configBuilder);

        var configuration = configBuilder.Build();
        var roleSection = role == PrimaryDatabaseRole.Runtime ? "Runtime" : "Migrator";
        var roleProvider = configuration[$"Database:{roleSection}:Provider"];
        var rootProvider = configuration["Database:Provider"];
        var explicitProvider = string.IsNullOrWhiteSpace(roleProvider)
            ? rootProvider
            : roleProvider;
        if (!string.IsNullOrWhiteSpace(explicitProvider)
            && !string.Equals(
                explicitProvider.Trim(),
                nameof(PrimaryDatabaseProvider.PostgreSql),
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var infisicalSecrets = infisicalAlreadyLoaded
            ? null
            : TryLoadInfisicalPostgresFolder(configuration, logger: null);
        var (host, _) = ResolveField(
            infisicalSecrets, InfisicalKeyHost, EnvHost, ConfigHost, configuration, infisicalAlreadyLoaded);
        var (port, _) = ResolveField(
            infisicalSecrets, InfisicalKeyPort, EnvPort, ConfigPort, configuration, infisicalAlreadyLoaded);
        var (database, _) = ResolveField(
            infisicalSecrets, InfisicalKeyDatabase, EnvDatabase, ConfigDatabase, configuration, infisicalAlreadyLoaded);
        var (username, _) = ResolveField(
            infisicalSecrets, InfisicalKeyUsername, EnvUsername, ConfigUsername, configuration, infisicalAlreadyLoaded);
        var (password, _) = ResolveField(
            infisicalSecrets, InfisicalKeyPassword, EnvPassword, ConfigPassword, configuration, infisicalAlreadyLoaded);

        if (string.IsNullOrWhiteSpace(host)
            && string.IsNullOrWhiteSpace(port)
            && string.IsNullOrWhiteSpace(database)
            && string.IsNullOrWhiteSpace(username)
            && string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var projected = new Dictionary<string, string?>();

        if (string.IsNullOrWhiteSpace(roleProvider)
            && string.IsNullOrWhiteSpace(rootProvider))
        {
            projected["Database:Provider"] = nameof(PrimaryDatabaseProvider.PostgreSql);
        }

        TryProject(projected, configuration, roleSection, "Host", host);
        TryProject(projected, configuration, roleSection, "Port", port);
        TryProject(projected, configuration, roleSection, "Database", database);
        TryProject(projected, configuration, roleSection, "Username", username, roleScoped: true);
        TryProject(projected, configuration, roleSection, "Password", password, roleScoped: true);

        if (projected.Count > 0)
        {
            configBuilder.AddInMemoryCollection(projected);
        }
    }

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

        var winningSource = DescribeWinningSource(
            hostSource, portSource, databaseSource, usernameSource, passwordSource);

        var connectionResult = PrimaryDatabaseConfiguration.BuildConnectionString(new PrimaryDatabaseConnectionOptions
        {
            Role = PrimaryDatabaseRole.Runtime,
            Provider = PrimaryDatabaseProvider.PostgreSql,
            Host = host!,
            Port = port,
            Database = database!,
            Username = username!,
            Password = password!,
            TlsMode = PrimaryDatabaseTlsMode.Prefer,
            TrustServerCertificate = false,
        });

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
            connectionResult.ConnectionString,
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
        IConfiguration configuration,
        bool mappedConfigurationFirst = false)
    {
        if (infisicalSecrets is not null
            && infisicalSecrets.TryGetValue(infisicalKey, out var infisicalValue)
            && !string.IsNullOrWhiteSpace(infisicalValue))
        {
            return (infisicalValue, $"Infisical:{InfisicalPath}/{infisicalKey}");
        }

        var mappedEnvironmentValue = configuration[envKey];
        if (mappedConfigurationFirst
            && !string.IsNullOrWhiteSpace(mappedEnvironmentValue))
        {
            return (mappedEnvironmentValue, $"IConfiguration:{envKey}");
        }

        var environmentValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return (environmentValue, $"EnvironmentVariable:{envKey}");
        }

        if (!mappedConfigurationFirst
            && !string.IsNullOrWhiteSpace(mappedEnvironmentValue))
        {
            return (mappedEnvironmentValue, $"IConfiguration:{envKey}");
        }

        var configValue = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(configValue))
        {
            return (configValue, $"IConfiguration:{configKey}");
        }

        return (null, "<unresolved>");
    }

    private static void TryProject(
        IDictionary<string, string?> projected,
        IConfiguration configuration,
        string roleSection,
        string key,
        string? value,
        bool roleScoped = false)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !string.IsNullOrWhiteSpace(configuration[$"Database:{roleSection}:{key}"])
            || !string.IsNullOrWhiteSpace(configuration[$"Database:{key}"]))
        {
            return;
        }

        var targetKey = roleScoped
            ? $"Database:{roleSection}:{key}"
            : $"Database:{key}";
        projected[targetKey] = value;
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
        // Read bare "Infisical:*" keys - the canonical convention used by the rest of
        // this repo (see Explore.Secrets.Extensions.ConfigurationBuilderExtensions.AddInfisical,
        // Explore.API/Blazor/MigrationService ConfigurationExtensions, and user-secrets docs
        // in docs/CONFIGURATION.md). We also accept the legacy "SecretProvider:Infisical:*"
        // prefix as a secondary fallback so both shapes work.
        var bareSection = configuration.GetSection("Infisical");
        var prefixedSection = configuration.GetSection(
            $"{Configuration.SecretProviderOptions.SectionName}:Infisical");

        var projectId = bareSection["ProjectId"] ?? prefixedSection["ProjectId"];
        var clientId = bareSection["ClientId"] ?? prefixedSection["ClientId"];
        var clientSecret = bareSection["ClientSecret"] ?? prefixedSection["ClientSecret"];
        var environment = bareSection["Environment"] ?? prefixedSection["Environment"] ?? "dev";
        var url = bareSection["Url"] ?? prefixedSection["Url"];

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
            var effectiveUrl = (url ?? "https://app.infisical.com").TrimEnd('/');
            Console.Error.WriteLine(
                $"[Bootstrap] Infisical bootstrap: host={effectiveUrl} project={projectId} env={environment} clientId={clientId[..Math.Min(8, clientId.Length)]}...");

            // We call Infisical's REST API directly instead of the Infisical.Sdk 3.x package:
            // the SDK (at 3.0.4) wraps a native FFI binary whose LoginAsync hangs for 100s
            // against self-hosted Infisical instances before erroring out, while the equivalent
            // REST endpoints respond in <500ms. The bootstrap path MUST be fast and reliable,
            // so we avoid the SDK here entirely.
            //
            // We also force IPv4 for the outbound socket: many self-hosted Infisical deployments
            // publish both A and AAAA records but only the IPv4 address is actually reachable
            // from operator workstations / CI runners. .NET's default Happy Eyeballs prefers
            // IPv6 and blocks the whole request to its Timeout when AAAA is black-holed; curl
            // survives because it tries IPv4 in parallel earlier. Pinning AddressFamily here
            // keeps the bootstrap path deterministic across networks.
            using var handler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(5),
                ConnectCallback = static async (context, cancellationToken) =>
                {
                    var addresses = await Dns.GetHostAddressesAsync(
                        context.DnsEndPoint.Host,
                        AddressFamily.InterNetwork,
                        cancellationToken).ConfigureAwait(false);
                    if (addresses.Length == 0)
                    {
                        throw new SocketException((int)SocketError.HostNotFound);
                    }

                    var socket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp)
                    { NoDelay = true };
                    try
                    {
                        await socket.ConnectAsync(
                            addresses,
                            context.DnsEndPoint.Port,
                            cancellationToken).ConfigureAwait(false);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                },
            };
            using var http = new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(10),
            };

            var loginResp = http.PostAsJsonAsync(
                $"{effectiveUrl}/api/v1/auth/universal-auth/login",
                new { clientId, clientSecret }).GetAwaiter().GetResult();
            if (!loginResp.IsSuccessStatusCode)
            {
                var body = loginResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.Error.WriteLine(
                    $"[Bootstrap] Infisical login HTTP {(int)loginResp.StatusCode}: {body}");
                return null;
            }

            var loginJson = loginResp.Content
                .ReadFromJsonAsync<InfisicalLoginResponse>()
                .GetAwaiter()
                .GetResult();
            var accessToken = loginJson?.AccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.Error.WriteLine("[Bootstrap] Infisical login returned empty accessToken.");
                return null;
            }

            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var listUrl =
                $"{effectiveUrl}/api/v3/secrets/raw"
                + $"?workspaceId={Uri.EscapeDataString(projectId)}"
                + $"&environment={Uri.EscapeDataString(environment)}"
                + $"&secretPath={Uri.EscapeDataString(InfisicalPath)}"
                + "&expandSecretReferences=true&recursive=false";

            var listResp = http.GetAsync(listUrl).GetAwaiter().GetResult();
            if (!listResp.IsSuccessStatusCode)
            {
                var body = listResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.Error.WriteLine(
                    $"[Bootstrap] Infisical list-secrets HTTP {(int)listResp.StatusCode}: {body}");
                return null;
            }

            var listJson = listResp.Content
                .ReadFromJsonAsync<InfisicalListSecretsResponse>()
                .GetAwaiter()
                .GetResult();
            if (listJson?.Secrets is null || listJson.Secrets.Count == 0)
            {
                Console.Error.WriteLine(
                    $"[Bootstrap] Infisical returned no secrets for path {InfisicalPath}.");
                return null;
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var secret in listJson.Secrets)
            {
                if (!string.IsNullOrEmpty(secret.SecretKey))
                {
                    dict[secret.SecretKey] = secret.SecretValue ?? string.Empty;
                }
            }

            Console.Error.WriteLine(
                $"[Bootstrap] Infisical bootstrap loaded {dict.Count} secrets from {InfisicalPath}.");
            logger?.LogInformation(
                "Infisical bootstrap loaded {Count} secrets from {Path}.",
                dict.Count,
                InfisicalPath);
            return dict;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "Infisical bootstrap failed; falling back to environment variables and IConfiguration.");
            // Bootstrap runs before DI/logging is wired (design-time EF tooling, Program.Main
            // before host build). Silent Infisical failure here is the single most common cause
            // of "no Postgres credentials could be resolved" - always surface it to stderr so the
            // operator can see WHY the chain fell through to env/config.
            Console.Error.WriteLine(
                $"[Bootstrap] Infisical fetch failed ({ex.GetType().Name}): {ex.Message}");
            if (ex.InnerException is not null)
            {
                Console.Error.WriteLine(
                    $"[Bootstrap]   inner ({ex.InnerException.GetType().Name}): "
                    + ex.InnerException.Message);
            }
            Console.Error.WriteLine(
                "[Bootstrap] Falling back to environment variables and IConfiguration.");
            return null;
        }
    }

    private sealed record InfisicalLoginResponse(
        [property: JsonPropertyName("accessToken")] string? AccessToken);

    private sealed record InfisicalListSecretsResponse(
        [property: JsonPropertyName("secrets")] List<InfisicalRawSecret>? Secrets);

    private sealed record InfisicalRawSecret(
        [property: JsonPropertyName("secretKey")] string? SecretKey,
        [property: JsonPropertyName("secretValue")] string? SecretValue);
}
