// ABOUTME: Keycloak container fixture for browser E2E BFF authentication tests.
// ABOUTME: Imports the deterministic ISLAMU test realm used by real OIDC login flows.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public sealed class BffKeycloakFixture : IAsyncInitializer, IAsyncDisposable
{
    private const string KeycloakImage = "quay.io/keycloak/keycloak:26.1.2";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);

    public const string RealmName = "ISLAMU";
    public const string TestClientId = "islamu-event-blazor";
    public const string TestClientSecret = "test-blazor-secret";

    private IContainer? _container;

    public string BaseUrl { get; private set; } = string.Empty;

    public string Authority { get; private set; } = string.Empty;
    public string MetadataAddress { get; private set; } = string.Empty;

    public Task<string> GetTestUserAccessTokenAsync(CancellationToken cancellationToken = default)
        => GetAccessTokenAsync("test-user", "test-user-password", cancellationToken);

    public async Task InitializeAsync()
    {
        var testRealmPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestAssets",
            "ISLAMU-realm.test.json");

        if (!File.Exists(testRealmPath))
        {
            throw new FileNotFoundException(
                $"Test realm export not found at '{testRealmPath}'. " +
                "Ensure ISLAMU-realm.test.json is included as E2E test content.");
        }

        _container = new ContainerBuilder(KeycloakImage)
            .WithPortBinding(8080, true)
            .WithResourceMapping(testRealmPath, "/opt/keycloak/data/import/")
            .WithCommand("start-dev", "--import-realm", "--http-port=8080")
            .WithEnvironment("KC_HEALTH_ENABLED", "true")
            .WithEnvironment("KC_HTTP_ENABLED", "true")
            .WithEnvironment("KEYCLOAK_ADMIN", "admin")
            .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request =>
                    request
                        .ForPath($"/realms/{RealmName}/.well-known/openid-configuration")
                        .ForPort(8080)
                        .ForStatusCode(HttpStatusCode.OK),
                    wait => wait.WithTimeout(StartupTimeout)))
            .Build();

        using var startupCts = new CancellationTokenSource(StartupTimeout);
        await _container.StartAsync(startupCts.Token);

        // Keycloak can return discovery before login endpoints are fully warmed.
        await Task.Delay(TimeSpan.FromSeconds(3));

        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(8080);

        BaseUrl = $"http://{host}:{port}";
        Authority = $"{BaseUrl}/realms/{RealmName}";
        MetadataAddress = $"{Authority}/.well-known/openid-configuration";
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private async Task<string> GetAccessTokenAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = TestClientId,
            ["client_secret"] = TestClientSecret,
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid profile email"
        };

        using var content = new FormUrlEncodedContent(requestBody);
        using var response = await httpClient.PostAsync(
            $"{Authority}/protocol/openid-connect/token",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to acquire Keycloak test user token. Status: {response.StatusCode}. Body: {errorBody}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
        {
            throw new InvalidOperationException("Keycloak test user token response did not include access_token.");
        }

        return tokenResponse.AccessToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
