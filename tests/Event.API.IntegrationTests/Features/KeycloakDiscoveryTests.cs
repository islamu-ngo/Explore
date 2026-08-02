// ABOUTME: Keycloak OIDC metadata discovery and realm configuration validation tests.
// ABOUTME: Verifies the containerized Keycloak serves correct OIDC metadata, JWKS, and realm structure.

using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

/// <summary>
/// Validates that the containerized Keycloak serves correct OIDC metadata,
/// JWKS keys, and realm configuration. Catches misconfigured realm exports
/// that would cause token validation failures in the API.
/// </summary>
[Category(TestCategories.Security)]
[NotInParallel("SecurityInfra")]
[ClassDataSource<SecurityInfrastructureFixture>(Shared = SharedType.PerAssembly)]
public class KeycloakDiscoveryTests : IDisposable
{
    private readonly SecurityInfrastructureFixture _infra;
    private readonly HttpClient _httpClient;

    public KeycloakDiscoveryTests(SecurityInfrastructureFixture infra)
    {
        _infra = infra;
        _httpClient = new HttpClient();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    #region OIDC Metadata Discovery

    [Test]
    public async Task OidcMetadataEndpoint_ShouldReturnValidDocument()
    {
        var response = await _httpClient.GetAsync(_infra.KeycloakMetadataAddress);

        response.IsSuccessStatusCode.Should().BeTrue("the OIDC metadata endpoint must be reachable");

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("issuer", out var issuer).Should().BeTrue();
        issuer.GetString().Should().Be(_infra.KeycloakAuthority,
            "the issuer in OIDC metadata must match the Keycloak realm URL");

        doc.RootElement.TryGetProperty("token_endpoint", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("jwks_uri", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("authorization_endpoint", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("response_types_supported", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("subject_types_supported", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("id_token_signing_alg_values_supported", out _).Should().BeTrue();
    }

    [Test]
    public async Task OidcMetadata_ShouldAdvertiseCodeFlow()
    {
        var metadata = await GetOidcMetadataAsync();

        metadata.TryGetProperty("response_types_supported", out var responseTypes).Should().BeTrue();
        var types = responseTypes.EnumerateArray().Select(t => t.GetString()).ToList();

        types.Should().Contain("code",
            "the OIDC provider must support authorization code flow for the BFF pattern");
    }

    [Test]
    public async Task OidcMetadata_ShouldAdvertiseROPCGrant()
    {
        var metadata = await GetOidcMetadataAsync();

        metadata.TryGetProperty("grant_types_supported", out var grantTypes).Should().BeTrue();
        var types = grantTypes.EnumerateArray().Select(t => t.GetString()).ToList();

        types.Should().Contain("password",
            "the OIDC provider must support ROPC grant for test token acquisition");
    }

    [Test]
    public async Task OidcMetadata_ShouldAdvertiseRS256Signing()
    {
        var metadata = await GetOidcMetadataAsync();

        metadata.TryGetProperty("id_token_signing_alg_values_supported", out var algs).Should().BeTrue();
        var algValues = algs.EnumerateArray().Select(a => a.GetString()).ToList();

        algValues.Should().Contain("RS256",
            "the OIDC provider must support RS256 for JWT signing (production requirement)");
    }

    #endregion

    #region JWKS Endpoint

    [Test]
    public async Task JwksEndpoint_ShouldReturnValidKeys()
    {
        var metadata = await GetOidcMetadataAsync();
        var jwksUri = metadata.GetProperty("jwks_uri").GetString();

        jwksUri.Should().NotBeNullOrEmpty();

        var response = await _httpClient.GetAsync(jwksUri!);
        response.IsSuccessStatusCode.Should().BeTrue();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("keys", out var keys).Should().BeTrue();
        var keyList = keys.EnumerateArray().ToList();
        keyList.Should().NotBeEmpty("at least one signing key must be available");

        foreach (var key in keyList)
        {
            key.TryGetProperty("kty", out _).Should().BeTrue("each key must have a key type");
            key.TryGetProperty("use", out var use).Should().BeTrue();
            use.GetString().Should().BeOneOf("sig", "enc",
                "Keycloak may publish both signing and encryption keys");
            key.TryGetProperty("kid", out _).Should().BeTrue("each key must have a key ID");
        }

        var hasRsaSigningKey = keyList.Any(key =>
            key.TryGetProperty("use", out var use) &&
            use.GetString() == "sig" &&
            key.TryGetProperty("kty", out var keyType) &&
            keyType.GetString() == "RSA");

        hasRsaSigningKey.Should().BeTrue(
            "at least one RSA signing key must be available for RS256 JWT validation");
    }

    [Test]
    public async Task JwksKeys_ShouldIncludeRSAKey()
    {
        var metadata = await GetOidcMetadataAsync();
        var jwksUri = metadata.GetProperty("jwks_uri").GetString();

        var response = await _httpClient.GetAsync(jwksUri!);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var keys = doc.RootElement.GetProperty("keys");
        var hasRsa = keys.EnumerateArray()
            .Any(k => k.TryGetProperty("kty", out var kty) && kty.GetString() == "RSA");

        hasRsa.Should().BeTrue("at least one RSA key must be present for RS256 JWT signing");
    }

    #endregion

    #region Token Endpoint Validation

    [Test]
    public async Task TokenEndpoint_ShouldBeReachable()
    {
        var metadata = await GetOidcMetadataAsync();
        var tokenEndpoint = metadata.GetProperty("token_endpoint").GetString();

        tokenEndpoint.Should().NotBeNullOrEmpty();

        var probeResponse = await _httpClient.PostAsync(tokenEndpoint!,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "invalid-client",
                ["client_secret"] = "invalid-secret"
            }));

        probeResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized,
            "the token endpoint must be reachable and reject invalid credentials with 401");
    }

    [Test]
    public async Task TokenEndpoint_ShouldRejectInvalidCredentials()
    {
        var act = async () => await _infra.TokenClient.GetAccessTokenAsync("nonexistent-user", "wrong-password");

        await act.Should().ThrowAsync<InvalidOperationException>(
            "invalid credentials must cause an InvalidOperationException in the token client");
    }

    #endregion

    #region Realm Configuration

    [Test]
    public async Task RealmWellKnown_ShouldReturnOpenIdConfiguration()
    {
        var response = await _httpClient.GetAsync(_infra.KeycloakMetadataAddress);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        contentType.Should().Be("application/json",
            "OIDC metadata must be served as JSON");
    }

    [Test]
    public async Task RealmEndpoint_ShouldReturnRealmInfo()
    {
        var response = await _httpClient.GetAsync(_infra.KeycloakMetadataAddress);

        response.IsSuccessStatusCode.Should().BeTrue();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("issuer").GetString().Should().Be(_infra.KeycloakAuthority);
        doc.RootElement.GetProperty("authorization_endpoint").GetString().Should()
            .Contain("/realms/ISLAMU/", "the imported ISLAMU realm must serve OIDC endpoints");
    }

    #endregion

    #region Token Issuer Consistency

    [Test]
    public async Task TokenIssuer_ShouldMatchMetadataIssuer()
    {
        var token = await _infra.TokenClient.GetAdminTokenAsync();
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var metadata = await GetOidcMetadataAsync();
        var metadataIssuer = metadata.GetProperty("issuer").GetString();

        jwt.Issuer.Should().Be(metadataIssuer,
            "the token issuer must exactly match the issuer in OIDC metadata");
    }

    #endregion

    #region Helpers

    private async Task<JsonElement> GetOidcMetadataAsync()
    {
        var response = await _httpClient.GetAsync(_infra.KeycloakMetadataAddress);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    #endregion
}
