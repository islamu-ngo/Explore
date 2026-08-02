// ABOUTME: Configuration provider that loads secrets from Infisical into IConfiguration.
// ABOUTME: Converts Infisical secrets to canonical .NET configuration keys.

namespace Explore.Secrets.Configuration;

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Configuration provider that loads secrets from Infisical via direct REST API calls.
/// Uses HttpClient instead of Infisical.Sdk (whose Rust FFI hangs against self-hosted instances).
/// Secrets are loaded during startup and optionally reloaded periodically.
/// </summary>
public sealed class InfisicalConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly InfisicalConfigurationSource _source;
    private string? _accessToken;
    private Timer? _reloadTimer;
    private bool _disposed;

    public InfisicalConfigurationProvider(InfisicalConfigurationSource source)
    {
        _source = source;
    }

    /// <inheritdoc />
    public override void Load()
    {
        try
        {
            LoadViaRestApi();

            if (_source.ReloadOnChange && _reloadTimer is null)
            {
                _reloadTimer = new Timer(
                    _ => ReloadViaRestApi(),
                    null,
                    _source.ReloadInterval,
                    _source.ReloadInterval);
            }
        }
        catch (Exception ex)
        {
            if (_source.ThrowOnFirstLoadFailure)
            {
                throw new InvalidOperationException(
                    $"Failed to load secrets from Infisical: {ex.Message}. " +
                    $"Ensure Infisical credentials are configured correctly. " +
                    $"Project: {_source.ProjectId}, Environment: {_source.Environment}",
                    ex);
            }

            Console.Error.WriteLine($"[Infisical] Warning: Failed to load secrets: {ex.Message}");
            if (ex.InnerException is not null)
            {
                Console.Error.WriteLine(
                    $"[Infisical]   inner ({ex.InnerException.GetType().Name}): {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Loads secrets using direct REST API calls instead of the Infisical.Sdk package.
    /// The SDK (3.0.4) wraps a native Rust FFI binary whose LoginAsync hangs for 100+ seconds
    /// against self-hosted Infisical instances. The REST endpoints respond in &lt;500ms.
    /// IPv4 is forced because many self-hosted deployments publish AAAA records that are
    /// unreachable, causing .NET's Happy Eyeballs to block until timeout.
    /// </summary>
    private void LoadViaRestApi()
    {
        Console.Error.WriteLine($"[Infisical] Loading secrets via REST API...");
        Console.Error.WriteLine($"[Infisical] URL: {_source.Url}");
        Console.Error.WriteLine($"[Infisical] ProjectId: {_source.ProjectId}");
        Console.Error.WriteLine($"[Infisical] Environment: {_source.Environment}");
        Console.Error.WriteLine($"[Infisical] Paths: {string.Join(", ", _source.Paths)}");

        using var handler = CreateIpv4Handler();
        using var http = new HttpClient(handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(15),
        };

        var effectiveUrl = _source.Url.TrimEnd('/');

        // Authenticate if we don't have a token yet
        if (string.IsNullOrEmpty(_accessToken))
        {
            Console.Error.WriteLine($"[Infisical] Authenticating with Universal Auth...");

            var loginResp = http.PostAsJsonAsync(
                $"{effectiveUrl}/api/v1/auth/universal-auth/login",
                new { clientId = _source.ClientId, clientSecret = _source.ClientSecret })
                .GetAwaiter().GetResult();

            if (!loginResp.IsSuccessStatusCode)
            {
                var body = loginResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new InvalidOperationException(
                    $"Infisical login failed HTTP {(int)loginResp.StatusCode}: {body}");
            }

            var loginJson = loginResp.Content
                .ReadFromJsonAsync<InfisicalLoginResponse>()
                .GetAwaiter().GetResult();

            _accessToken = loginJson?.AccessToken;
            if (string.IsNullOrEmpty(_accessToken))
            {
                throw new InvalidOperationException("Infisical login returned empty accessToken.");
            }

            Console.Error.WriteLine($"[Infisical] Authentication successful!");
        }

        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var newData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var totalSecrets = 0;

        foreach (var path in _source.Paths)
        {
            Console.Error.WriteLine($"[Infisical] Loading secrets from path: {path}");

            var listUrl =
                $"{effectiveUrl}/api/v3/secrets/raw"
                + $"?workspaceId={Uri.EscapeDataString(_source.ProjectId)}"
                + $"&environment={Uri.EscapeDataString(_source.Environment)}"
                + $"&secretPath={Uri.EscapeDataString(path)}"
                + "&expandSecretReferences=true&recursive=true";

            var listResp = http.GetAsync(listUrl).GetAwaiter().GetResult();

            if (!listResp.IsSuccessStatusCode)
            {
                var body = listResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.Error.WriteLine(
                    $"[Infisical] Warning: list-secrets HTTP {(int)listResp.StatusCode} for path {path}: {body}");
                continue;
            }

            var listJson = listResp.Content
                .ReadFromJsonAsync<InfisicalListSecretsResponse>()
                .GetAwaiter().GetResult();

            if (listJson?.Secrets is null || listJson.Secrets.Count == 0)
            {
                Console.Error.WriteLine($"[Infisical] No secrets found in path: {path}");
                continue;
            }

            Console.Error.WriteLine($"[Infisical] Found {listJson.Secrets.Count} secrets in path: {path}");
            foreach (var secret in listJson.Secrets)
            {
                if (string.IsNullOrEmpty(secret.SecretKey)) continue;

                // Convert to .NET configuration key format
                var configKey = ConvertToConfigurationKey(secret.SecretKey, path);
                newData[configKey] = secret.SecretValue ?? string.Empty;

                // Also store with original key for direct access
                newData[secret.SecretKey] = secret.SecretValue ?? string.Empty;

                Console.Error.WriteLine($"[Infisical]   - {secret.SecretKey} -> {configKey}");
                totalSecrets++;
            }
        }

        Console.Error.WriteLine($"[Infisical] Total secrets loaded: {totalSecrets}");
        Data = newData;
    }

    private void ReloadViaRestApi()
    {
        try
        {
            // Clear token to force re-authentication on reload
            _accessToken = null;
            LoadViaRestApi();
            OnReload();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Infisical] Warning: Failed to reload secrets: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an HttpHandler that forces IPv4 connections.
    /// Self-hosted Infisical often publishes AAAA records that are unreachable;
    /// .NET's Happy Eyeballs prefers IPv6 and blocks until timeout.
    /// </summary>
    private static SocketsHttpHandler CreateIpv4Handler() => new()
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

    private sealed record InfisicalLoginResponse(
        [property: JsonPropertyName("accessToken")] string? AccessToken);

    private sealed record InfisicalListSecretsResponse(
        [property: JsonPropertyName("secrets")] List<InfisicalRawSecret>? Secrets);

    private sealed record InfisicalRawSecret(
        [property: JsonPropertyName("secretKey")] string? SecretKey,
        [property: JsonPropertyName("secretValue")] string? SecretValue);

    /// <summary>
    /// Converts an Infisical secret key to .NET configuration format.
    /// </summary>
    /// <remarks>
    /// Conversion rules:
    /// - Path becomes section prefix: "/keycloak" -> "Keycloak:"
    /// - SCREAMING_SNAKE_CASE becomes PascalCase
    /// - Double underscores become colons (subsections)
    ///
    /// Examples:
    /// - "/keycloak/KEYCLOAK_REALM" -> "Keycloak:Realm"
    /// - "/api/S3__ACCESS_KEY" -> "S3:AccessKey"
    /// </remarks>
    private static string ConvertToConfigurationKey(string secretKey, string path)
    {
        // Special mappings for common patterns
        if (secretKey.Equals("AI_TOOL_PROPOSALS_ENABLED", StringComparison.OrdinalIgnoreCase))
        {
            return "AiProvider:ToolProposalsEnabled";
        }

        // Normalize path to get section name
        var section = path.Trim('/');
        if (string.IsNullOrEmpty(section))
        {
            section = string.Empty;
        }
        else
        {
            section = ToPascalCase(section) + ":";
        }

        // Remove section prefix from key if present
        var keyWithoutSection = secretKey;
        var sectionUpper = section.TrimEnd(':').ToUpperInvariant();
        if (!string.IsNullOrEmpty(sectionUpper) &&
            secretKey.StartsWith(sectionUpper + "_", StringComparison.OrdinalIgnoreCase))
        {
            keyWithoutSection = secretKey[(sectionUpper.Length + 1)..];
        }

        // Handle double underscore as subsection separator
        var parts = keyWithoutSection.Split("__", StringSplitOptions.RemoveEmptyEntries);
        var configParts = parts.Select(ToPascalCase);
        var configKey = string.Join(":", configParts);

        return section + configKey;
    }

    /// <summary>
    /// Converts SCREAMING_SNAKE_CASE to PascalCase.
    /// </summary>
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var parts = input.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var pascalParts = parts.Select(part =>
        {
            if (part.Length == 0) return string.Empty;
            return char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
        });

        return string.Join("", pascalParts);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _reloadTimer?.Dispose();
        _disposed = true;
    }
}
