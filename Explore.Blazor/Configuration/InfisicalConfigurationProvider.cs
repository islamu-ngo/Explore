// ABOUTME: Loads Blazor startup secrets directly from the Infisical REST API.
// ABOUTME: Maps provider secret names into the configuration keys consumed by the BFF host.

namespace Explore.Blazor.Configuration;

using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

public sealed class InfisicalConfigurationProvider(InfisicalConfigurationSource source)
    : ConfigurationProvider
{
    public override void Load()
    {
        try
        {
            Data = LoadSecrets();
        }
        catch (Exception ex)
        {
            if (source.ThrowOnFirstLoadFailure)
            {
                throw new InvalidOperationException(
                    $"Failed to load Blazor startup secrets from Infisical for project "
                    + $"{source.ProjectId} in environment {source.Environment}.",
                    ex);
            }

            Console.Error.WriteLine(
                $"[Infisical] Warning: Blazor startup secret loading failed ({ex.GetType().Name}).");
        }
    }

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
            {
                NoDelay = true,
            };

            try
            {
                await socket.ConnectAsync(
                    addresses[0],
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

    private Dictionary<string, string?> LoadSecrets()
    {
        using var handler = CreateIpv4Handler();
        using var http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15),
        };

        var baseUrl = source.Url.TrimEnd('/');
        var loginResponse = http.PostAsJsonAsync(
                $"{baseUrl}/api/v1/auth/universal-auth/login",
                new { clientId = source.ClientId, clientSecret = source.ClientSecret })
            .GetAwaiter()
            .GetResult();

        if (!loginResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Infisical Universal Auth failed with HTTP {(int)loginResponse.StatusCode}.");
        }

        var login = loginResponse.Content
            .ReadFromJsonAsync<InfisicalLoginResponse>()
            .GetAwaiter()
            .GetResult();

        if (string.IsNullOrEmpty(login?.AccessToken))
        {
            throw new InvalidOperationException("Infisical Universal Auth returned no access token.");
        }

        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in source.Paths)
        {
            var requestUrl =
                $"{baseUrl}/api/v3/secrets/raw"
                + $"?workspaceId={Uri.EscapeDataString(source.ProjectId)}"
                + $"&environment={Uri.EscapeDataString(source.Environment)}"
                + $"&secretPath={Uri.EscapeDataString(path)}"
                + "&expandSecretReferences=true&recursive=true";

            var response = http.GetAsync(requestUrl).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine(
                    $"[Infisical] Warning: HTTP {(int)response.StatusCode} while loading path {path}.");
                continue;
            }

            var payload = response.Content
                .ReadFromJsonAsync<InfisicalListSecretsResponse>()
                .GetAwaiter()
                .GetResult();

            foreach (var secret in payload?.Secrets ?? [])
            {
                if (string.IsNullOrEmpty(secret.SecretKey))
                {
                    continue;
                }

                data[ConvertToConfigurationKey(secret.SecretKey, path)] = secret.SecretValue ?? string.Empty;
                data[secret.SecretKey] = secret.SecretValue ?? string.Empty;
            }
        }

        return data;
    }

    private static string ConvertToConfigurationKey(string secretKey, string path)
    {
        if (secretKey.Equals("AI_TOOL_PROPOSALS_ENABLED", StringComparison.OrdinalIgnoreCase))
        {
            return "AiProvider:ToolProposalsEnabled";
        }

        var sectionName = path.Trim('/');
        var section = string.IsNullOrEmpty(sectionName)
            ? string.Empty
            : ToPascalCase(sectionName) + ":";
        var sectionPrefix = section.TrimEnd(':').ToUpperInvariant();
        var key = !string.IsNullOrEmpty(sectionPrefix)
            && secretKey.StartsWith(sectionPrefix + "_", StringComparison.OrdinalIgnoreCase)
                ? secretKey[(sectionPrefix.Length + 1)..]
                : secretKey;

        return section + string.Join(
            ":",
            key.Split("__", StringSplitOptions.RemoveEmptyEntries).Select(ToPascalCase));
    }

    private static string ToPascalCase(string value) => string.Concat(
        value.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(static part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));

    private sealed record InfisicalLoginResponse(
        [property: JsonPropertyName("accessToken")] string? AccessToken);

    private sealed record InfisicalListSecretsResponse(
        [property: JsonPropertyName("secrets")] List<InfisicalRawSecret>? Secrets);

    private sealed record InfisicalRawSecret(
        [property: JsonPropertyName("secretKey")] string? SecretKey,
        [property: JsonPropertyName("secretValue")] string? SecretValue);
}
