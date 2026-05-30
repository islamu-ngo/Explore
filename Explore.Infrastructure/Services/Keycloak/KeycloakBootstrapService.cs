// ABOUTME: Implements setup-time Keycloak realm and client bootstrap through the Keycloak Admin API.
// ABOUTME: Keeps admin credentials and provider response bodies out of persisted state, logs, and result DTOs.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Onboarding;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Services.Keycloak;

public sealed class KeycloakBootstrapService : IKeycloakBootstrapService
{
    public const string HttpClientName = "KeycloakBootstrapClient";

    private const string AdminCliClientId = "admin-cli";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeycloakBootstrapService> _logger;

    public KeycloakBootstrapService(
        IHttpClientFactory httpClientFactory,
        ILogger<KeycloakBootstrapService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<KeycloakBootstrapResultDto> BootstrapAsync(
        KeycloakBootstrapRequestDto request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryNormalizeBaseUri(request.KeycloakBaseUrl, out var baseUri, out var failureCode))
        {
            return Failure(request, failureCode, "Keycloak base URL is not safe for bootstrap.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var accessToken = await RequestAdminTokenAsync(client, baseUri!, request, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Failure(request, "keycloak_auth_failed", "Keycloak admin authentication failed.");
            }

            var realmStatus = await EnsureRealmAsync(client, baseUri!, request, accessToken, cancellationToken);
            if (!realmStatus.Success)
                return realmStatus;

            var blazorUpdated = await EnsureClientSecretAsync(
                client,
                baseUri!,
                request.Realm,
                request.BlazorClientId,
                request.BlazorClientSecret,
                accessToken,
                bearerOnly: false,
                cancellationToken);

            if (!blazorUpdated.Success)
                return Failure(request, blazorUpdated.FailureCode, blazorUpdated.Message, realmStatus.RealmCreated);

            var apiUpdated = false;
            if (!string.IsNullOrWhiteSpace(request.ApiClientId) && !string.IsNullOrWhiteSpace(request.ApiClientSecret))
            {
                var apiUpdate = await EnsureClientSecretAsync(
                    client,
                    baseUri!,
                    request.Realm,
                    request.ApiClientId,
                    request.ApiClientSecret,
                    accessToken,
                    bearerOnly: true,
                    cancellationToken);

                if (!apiUpdate.Success)
                    return Failure(request, apiUpdate.FailureCode, apiUpdate.Message, realmStatus.RealmCreated, blazorClientUpdated: true);

                apiUpdated = true;
            }

            _logger.LogInformation(
                "Keycloak bootstrap Admin API calls completed. Realm: {Realm}, BlazorClientId: {BlazorClientId}, ApiClientId: {ApiClientId}, Mode: {Mode}, RealmCreated: {RealmCreated}, ApiClientUpdated: {ApiClientUpdated}",
                request.Realm,
                request.BlazorClientId,
                request.ApiClientId,
                request.Mode,
                realmStatus.RealmCreated,
                apiUpdated);

            return new KeycloakBootstrapResultDto
            {
                Success = true,
                Message = "Keycloak bootstrap completed successfully.",
                Mode = request.Mode,
                Realm = request.Realm,
                BlazorClientId = request.BlazorClientId,
                ApiClientId = request.ApiClientId,
                RealmCreated = realmStatus.RealmCreated,
                BlazorClientUpdated = true,
                ApiClientUpdated = apiUpdated
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Failure(request, "keycloak_timeout", "Keycloak bootstrap timed out before completion.");
        }
        catch (HttpRequestException)
        {
            return Failure(request, "keycloak_unreachable", "Keycloak Admin API was unreachable during bootstrap.");
        }
        catch (JsonException)
        {
            return Failure(request, "keycloak_invalid_response", "Keycloak Admin API returned an invalid response.");
        }
    }

    private static bool TryNormalizeBaseUri(string keycloakBaseUrl, out Uri? baseUri, out string failureCode)
    {
        baseUri = null;
        failureCode = "keycloak_invalid_url";

        if (!Uri.TryCreate(keycloakBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        if (IsBlockedHost(uri.Host))
        {
            failureCode = "keycloak_unsafe_host";
            return false;
        }

        baseUri = new Uri(uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/");
        return true;
    }

    private static bool IsBlockedHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var address))
            return false;

        if (IPAddress.IsLoopback(address))
            return true;

        if (address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.None)
            || address.Equals(IPAddress.IPv6None))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        return address.AddressFamily switch
        {
            System.Net.Sockets.AddressFamily.InterNetwork =>
                bytes[0] == 169 && bytes[1] == 254 || bytes[0] >= 224,
            System.Net.Sockets.AddressFamily.InterNetworkV6 =>
                address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal,
            _ => true
        };
    }

    private static Uri BuildUri(Uri baseUri, params string[] segments)
    {
        var existingPath = baseUri.AbsolutePath.Trim('/');
        var pathSegments = segments.Select(segment => Uri.EscapeDataString(segment.Trim('/')));
        var path = string.Join('/', string.IsNullOrEmpty(existingPath)
            ? pathSegments
            : [existingPath, .. pathSegments]);

        return new UriBuilder(baseUri)
        {
            Path = path
        }.Uri;
    }

    private static Uri BuildClientLookupUri(Uri baseUri, string realm, string clientId)
    {
        var builder = new UriBuilder(BuildUri(baseUri, "admin", "realms", realm, "clients"))
        {
            Query = $"clientId={Uri.EscapeDataString(clientId)}"
        };

        return builder.Uri;
    }

    private static async Task<string?> RequestAdminTokenAsync(
        HttpClient client,
        Uri baseUri,
        KeycloakBootstrapRequestDto request,
        CancellationToken cancellationToken)
    {
        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            BuildUri(baseUri, "realms", "master", "protocol", "openid-connect", "token"))
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = AdminCliClientId,
                ["username"] = request.BootstrapAdminUsername,
                ["password"] = request.BootstrapAdminPassword
            })
        };

        using var response = await client.SendAsync(tokenRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var token = await JsonSerializer.DeserializeAsync<KeycloakTokenResponse>(stream, JsonOptions, cancellationToken);
        return token?.AccessToken;
    }

    private async Task<KeycloakBootstrapResultDto> EnsureRealmAsync(
        HttpClient client,
        Uri baseUri,
        KeycloakBootstrapRequestDto request,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var checkRequest = CreateAdminRequest(
            HttpMethod.Get,
            BuildUri(baseUri, "admin", "realms", request.Realm),
            accessToken);
        using var checkResponse = await client.SendAsync(checkRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (checkResponse.IsSuccessStatusCode)
            return Success(request, realmCreated: false);

        if (checkResponse.StatusCode != HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Keycloak realm check failed. Realm: {Realm}, Mode: {Mode}, StatusCode: {StatusCode}",
                request.Realm,
                request.Mode,
                (int)checkResponse.StatusCode);

            return Failure(request, "keycloak_realm_check_failed", "Keycloak realm status could not be verified.");
        }

        if (request.Mode != KeycloakBootstrapMode.CreateRealm)
            return Failure(request, "keycloak_realm_not_found", "Keycloak realm was not found and create mode was not requested.");

        using var createRequest = CreateAdminRequest(
            HttpMethod.Post,
            BuildUri(baseUri, "admin", "realms"),
            accessToken,
            new KeycloakRealmRepresentation(request.Realm, Enabled: true));
        using var createResponse = await client.SendAsync(createRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (createResponse.IsSuccessStatusCode || createResponse.StatusCode == HttpStatusCode.Conflict)
            return Success(request, realmCreated: createResponse.StatusCode != HttpStatusCode.Conflict);

        _logger.LogWarning(
            "Keycloak realm create failed. Realm: {Realm}, StatusCode: {StatusCode}",
            request.Realm,
            (int)createResponse.StatusCode);

        return Failure(request, "keycloak_realm_create_failed", "Keycloak realm could not be created.");
    }

    private async Task<ClientSecretUpdateResult> EnsureClientSecretAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientId,
        string secret,
        string accessToken,
        bool bearerOnly,
        CancellationToken cancellationToken)
    {
        var lookup = await FindClientAsync(client, baseUri, realm, clientId, accessToken, cancellationToken);
        if (!lookup.Success)
            return ClientSecretUpdateResult.Failure("keycloak_client_lookup_failed", "Keycloak client lookup failed.");

        var clientUuid = lookup.ClientUuid;
        if (clientUuid is null)
        {
            var createResult = await CreateClientAsync(client, baseUri, realm, clientId, accessToken, bearerOnly, cancellationToken);
            if (!createResult.Success)
                return createResult;

            lookup = await FindClientAsync(client, baseUri, realm, clientId, accessToken, cancellationToken);
            if (!lookup.Success)
                return ClientSecretUpdateResult.Failure("keycloak_client_lookup_failed", "Keycloak client lookup failed.");

            clientUuid = lookup.ClientUuid;
            if (clientUuid is null)
                return ClientSecretUpdateResult.Failure("keycloak_client_not_found", "Keycloak client could not be located after creation.");
        }

        using var secretRequest = CreateAdminRequest(
            HttpMethod.Put,
            BuildUri(baseUri, "admin", "realms", realm, "clients", clientUuid, "client-secret"),
            accessToken,
            new KeycloakClientSecretRepresentation("secret", secret));
        using var secretResponse = await client.SendAsync(secretRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (secretResponse.IsSuccessStatusCode)
            return ClientSecretUpdateResult.Succeeded();

        _logger.LogWarning(
            "Keycloak client secret update failed. Realm: {Realm}, ClientId: {ClientId}, StatusCode: {StatusCode}",
            realm,
            clientId,
            (int)secretResponse.StatusCode);

        return ClientSecretUpdateResult.Failure(
            "keycloak_client_secret_update_failed",
            "Keycloak client secret could not be updated.");
    }

    private static async Task<ClientLookupResult> FindClientAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAdminRequest(
            HttpMethod.Get,
            BuildClientLookupUri(baseUri, realm, clientId),
            accessToken);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return ClientLookupResult.Failure();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var clients = await JsonSerializer.DeserializeAsync<List<KeycloakClientLookupResult>>(stream, JsonOptions, cancellationToken);
        var clientUuid = clients?.FirstOrDefault(x => string.Equals(x.ClientId, clientId, StringComparison.Ordinal))?.Id;
        return ClientLookupResult.Succeeded(clientUuid);
    }

    private async Task<ClientSecretUpdateResult> CreateClientAsync(
        HttpClient client,
        Uri baseUri,
        string realm,
        string clientId,
        string accessToken,
        bool bearerOnly,
        CancellationToken cancellationToken)
    {
        var representation = bearerOnly
            ? KeycloakClientRepresentation.CreateBearerOnly(clientId)
            : KeycloakClientRepresentation.CreateConfidentialOidc(clientId);
        using var request = CreateAdminRequest(
            HttpMethod.Post,
            BuildUri(baseUri, "admin", "realms", realm, "clients"),
            accessToken,
            representation);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
            return ClientSecretUpdateResult.Succeeded();

        _logger.LogWarning(
            "Keycloak client create failed. Realm: {Realm}, ClientId: {ClientId}, StatusCode: {StatusCode}",
            realm,
            clientId,
            (int)response.StatusCode);

        return ClientSecretUpdateResult.Failure("keycloak_client_create_failed", "Keycloak client could not be created.");
    }

    private static HttpRequestMessage CreateAdminRequest(
        HttpMethod method,
        Uri requestUri,
        string accessToken,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        return request;
    }

    private static KeycloakBootstrapResultDto Success(KeycloakBootstrapRequestDto request, bool realmCreated)
    {
        return new KeycloakBootstrapResultDto
        {
            Success = true,
            Mode = request.Mode,
            Realm = request.Realm,
            BlazorClientId = request.BlazorClientId,
            ApiClientId = request.ApiClientId,
            RealmCreated = realmCreated
        };
    }

    private static KeycloakBootstrapResultDto Failure(
        KeycloakBootstrapRequestDto request,
        string failureCode,
        string message,
        bool realmCreated = false,
        bool blazorClientUpdated = false)
    {
        return new KeycloakBootstrapResultDto
        {
            Success = false,
            Message = message,
            FailureCode = failureCode,
            Mode = request.Mode,
            Realm = request.Realm,
            BlazorClientId = request.BlazorClientId,
            ApiClientId = request.ApiClientId,
            RealmCreated = realmCreated,
            BlazorClientUpdated = blazorClientUpdated
        };
    }

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed record KeycloakRealmRepresentation(string Realm, bool Enabled);

    private sealed class KeycloakClientLookupResult
    {
        public string Id { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
    }

    private sealed record KeycloakClientSecretRepresentation(string Type, string Value);

    private sealed class KeycloakClientRepresentation
    {
        public string ClientId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool Enabled { get; init; } = true;
        public string Protocol { get; init; } = "openid-connect";
        public bool PublicClient { get; init; }
        public bool StandardFlowEnabled { get; init; }
        public bool DirectAccessGrantsEnabled { get; init; }
        public bool ServiceAccountsEnabled { get; init; }
        public bool BearerOnly { get; init; }

        public static KeycloakClientRepresentation CreateConfidentialOidc(string clientId) => new()
        {
            ClientId = clientId,
            Name = clientId,
            PublicClient = false,
            StandardFlowEnabled = true,
            DirectAccessGrantsEnabled = false,
            ServiceAccountsEnabled = false,
            BearerOnly = false
        };

        public static KeycloakClientRepresentation CreateBearerOnly(string clientId) => new()
        {
            ClientId = clientId,
            Name = clientId,
            PublicClient = false,
            StandardFlowEnabled = false,
            DirectAccessGrantsEnabled = false,
            ServiceAccountsEnabled = false,
            BearerOnly = true
        };
    }

    private sealed record ClientSecretUpdateResult(bool Success, string FailureCode, string Message)
    {
        public static ClientSecretUpdateResult Succeeded() => new(true, string.Empty, string.Empty);

        public static ClientSecretUpdateResult Failure(string failureCode, string message) => new(false, failureCode, message);
    }

    private sealed record ClientLookupResult(bool Success, string? ClientUuid)
    {
        public static ClientLookupResult Succeeded(string? clientUuid) => new(true, clientUuid);

        public static ClientLookupResult Failure() => new(false, null);
    }
}
