// ABOUTME: Configuration provider that loads secrets from Infisical into IConfiguration.
// Converts Infisical secrets to .NET configuration keys (SCREAMING_SNAKE_CASE to Section:PascalCase).

namespace Explore.Secrets.Configuration;

using Infisical.Sdk;
using Infisical.Sdk.Model;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Configuration provider that loads secrets from Infisical.
/// Secrets are loaded during startup and optionally reloaded periodically.
/// </summary>
public sealed class InfisicalConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly InfisicalConfigurationSource _source;
    private InfisicalClient? _client;
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
            LoadAsync().GetAwaiter().GetResult();

            if (_source.ReloadOnChange && _reloadTimer is null)
            {
                _reloadTimer = new Timer(
                    _ => ReloadAsync().GetAwaiter().GetResult(),
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

            // Log but continue with empty configuration
            Console.Error.WriteLine($"[Infisical] Warning: Failed to load secrets: {ex.Message}");
        }
    }

    private async Task LoadAsync()
    {
        Console.WriteLine($"[Infisical] Starting LoadAsync...");
        Console.WriteLine($"[Infisical] URL: {_source.Url}");
        Console.WriteLine($"[Infisical] ProjectId: {_source.ProjectId}");
        Console.WriteLine($"[Infisical] Environment: {_source.Environment}");
        Console.WriteLine($"[Infisical] Paths: {string.Join(", ", _source.Paths)}");

        // Initialize client if not already done
        if (_client is null)
        {
            Console.WriteLine($"[Infisical] Initializing client...");
            var settings = new InfisicalSdkSettingsBuilder()
                .WithHostUri(_source.Url)
                .Build();

            _client = new InfisicalClient(settings);

            Console.WriteLine($"[Infisical] Authenticating with Universal Auth...");
            await _client.Auth().UniversalAuth().LoginAsync(
                _source.ClientId,
                _source.ClientSecret);
            Console.WriteLine($"[Infisical] Authentication successful!");
        }

        var newData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var totalSecrets = 0;

        foreach (var path in _source.Paths)
        {
            Console.WriteLine($"[Infisical] Loading secrets from path: {path}");
            var options = new ListSecretsOptions
            {
                ProjectId = _source.ProjectId,
                EnvironmentSlug = _source.Environment,
                SecretPath = path,
                Recursive = true,
                ExpandSecretReferences = true,
                ViewSecretValue = true
            };

            var secrets = await _client.Secrets().ListAsync(options);

            if (secrets is null)
            {
                Console.WriteLine($"[Infisical] No secrets found in path: {path}");
                continue;
            }

            Console.WriteLine($"[Infisical] Found {secrets.Length} secrets in path: {path}");
            foreach (var secret in secrets)
            {
                // Convert to .NET configuration key format
                var configKey = ConvertToConfigurationKey(secret.SecretKey, path);
                newData[configKey] = secret.SecretValue;

                // Also store with original key for direct access
                newData[secret.SecretKey] = secret.SecretValue;

                // Log the key (not the value!)
                Console.WriteLine($"[Infisical]   - {secret.SecretKey} -> {configKey}");
                totalSecrets++;
            }
        }

        Console.WriteLine($"[Infisical] Total secrets loaded: {totalSecrets}");
        Data = newData;
    }

    private async Task ReloadAsync()
    {
        try
        {
            await LoadAsync();
            OnReload();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Infisical] Warning: Failed to reload secrets: {ex.Message}");
        }
    }

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
    /// - "/postgresql/POSTGRESQL_PUBLIC_URL" -> "ConnectionStrings:DefaultConnection" (special case)
    /// - "/api/S3__ACCESS_KEY" -> "S3:AccessKey"
    /// </remarks>
    private static string ConvertToConfigurationKey(string secretKey, string path)
    {
        // Special mappings for common patterns
        if (secretKey.Equals("POSTGRESQL_PUBLIC_URL", StringComparison.OrdinalIgnoreCase))
        {
            return "ConnectionStrings:DefaultConnection";
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

        if (_client is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _disposed = true;
    }
}
