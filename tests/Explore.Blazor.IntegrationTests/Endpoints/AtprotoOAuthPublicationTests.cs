// ABOUTME: Integration tests for AT Protocol OAuth client metadata and public JWKS publication.
// ABOUTME: Proves the BFF publishes canonical confidential-client documents without redirects or private key material.

using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Explore.Blazor.IntegrationTests.Fixtures;
using Explore.Blazor.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class AtprotoOAuthPublicationTests
{
    [Test]
    public async Task ClientMetadataPublishesExactConfidentialClientContract()
    {
        await using var factory = CreateFactory(CreatePrivateJwks(("oauth-client-2026-07", "active")));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://events.example.com")
        });

        using var response = await client.GetAsync("/oauth/client-metadata.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
        response.Headers.CacheControl?.Public.Should().BeTrue();
        response.Headers.CacheControl?.MaxAge.Should().Be(TimeSpan.FromMinutes(5));

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("client_id").GetString().Should().Be("https://events.example.com/oauth/client-metadata.json");
        root.GetProperty("redirect_uris")[0].GetString().Should().Be("https://events.example.com/signin-atproto");
        root.GetProperty("scope").GetString().Should().Be("atproto transition:generic");
        root.GetProperty("response_types").EnumerateArray().Select(value => value.GetString())
            .Should().Equal("code");
        root.GetProperty("grant_types").EnumerateArray().Select(value => value.GetString())
            .Should().Equal("authorization_code", "refresh_token");
        root.GetProperty("dpop_bound_access_tokens").GetBoolean().Should().BeTrue();
        root.GetProperty("token_endpoint_auth_method").GetString().Should().Be("private_key_jwt");
        root.GetProperty("token_endpoint_auth_signing_alg").GetString().Should().Be("ES256");
        root.GetProperty("jwks_uri").GetString().Should().Be("https://events.example.com/oauth/jwks.json");
        root.TryGetProperty("jwks", out _).Should().BeFalse();
    }

    [Test]
    public async Task JwksPublishesActiveAndRetiredPublicKeysInStableOrderWithoutPrivateMaterial()
    {
        var privateRing = CreatePrivateJwks(("z-active", "active"), ("a-retired", "retired"));
        await using var factory = CreateFactory(privateRing);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://events.example.com")
        });

        using var response = await client.GetAsync("/oauth/jwks.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var keys = document.RootElement.GetProperty("keys").EnumerateArray().ToArray();
        keys.Select(key => key.GetProperty("kid").GetString()).Should().Equal("a-retired", "z-active");
        foreach (var key in keys)
        {
            key.EnumerateObject().Select(property => property.Name)
                .Should().BeEquivalentTo("kty", "crv", "x", "y", "kid", "use", "alg");
            key.GetProperty("kty").GetString().Should().Be("EC");
            key.GetProperty("crv").GetString().Should().Be("P-256");
            key.GetProperty("use").GetString().Should().Be("sig");
            key.GetProperty("alg").GetString().Should().Be("ES256");
            key.TryGetProperty("d", out _).Should().BeFalse();
            key.TryGetProperty("status", out _).Should().BeFalse();
        }
    }

    [Test]
    public async Task PublicationOnNonCanonicalHostReturnsNotFoundWithoutRedirect()
    {
        await using var factory = CreateFactory(CreatePrivateJwks(("active", "active")));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://alternate.example.com")
        });

        using var response = await client.GetAsync("/oauth/client-metadata.json");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Location.Should().BeNull();
    }

    [Test]
    [Arguments("/oauth/client-metadata.json")]
    [Arguments("/oauth/jwks.json")]
    public async Task OAuthPublicationOverHttpFailsClosedWithoutRedirect(string endpointPath)
    {
        await using var factory = CreateFactory(CreatePrivateJwks(("active", "active")));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://events.example.com")
        });

        using var response = await client.GetAsync(endpointPath);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Location.Should().BeNull();
    }

    [Test]
    [Arguments("https://user:password@events.example.com", "https://events.example.com")]
    [Arguments("https://events.example.com.", "https://events.example.com.")]
    [Arguments("https://évents.example.com", "https://évents.example.com")]
    public async Task ClientMetadataRejectsNonCanonicalOrCredentialBearingPublicAuthority(
        string publicUrl,
        string requestBaseAddress)
    {
        await using var factory = CreateFactory(
            CreatePrivateJwks(("active", "active")),
            publicUrl: publicUrl);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri(requestBaseAddress)
        });

        using var response = await client.GetAsync("/oauth/client-metadata.json");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Location.Should().BeNull();
    }

    [Test]
    public async Task ClientMetadataNormalizesExplicitDefaultHttpsPort()
    {
        await using var factory = CreateFactory(
            CreatePrivateJwks(("active", "active")),
            publicUrl: "https://events.example.com:443");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://events.example.com")
        });

        using var response = await client.GetAsync("/oauth/client-metadata.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("client_id").GetString()
            .Should().Be("https://events.example.com/oauth/client-metadata.json");
    }

    [Test]
    [Arguments("//evil.example/callback")]
    [Arguments("https://evil.example/callback")]
    [Arguments("signin-atproto")]
    [Arguments("/\\evil.example/callback")]
    [Arguments("\\\\evil.example\\callback")]
    [Arguments("/signin-atproto?next=https://evil.example")]
    [Arguments("/signin-atproto#fragment")]
    [Arguments("/oauth/../signin-atproto")]
    [Arguments("/oauth/%2e%2e/signin-atproto")]
    [Arguments("/signin%2fatproto")]
    public async Task ClientMetadataRejectsNonLocalOrAmbiguousCallbackPath(string callbackPath)
    {
        await using var factory = CreateFactory(
            CreatePrivateJwks(("active", "active")),
            callbackPath);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://events.example.com")
        });

        using var response = await client.GetAsync("/oauth/client-metadata.json");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Location.Should().BeNull();
    }

    [Test]
    public void KeyProviderRejectsMissingMalformedUnknownDuplicateAndInvalidActiveKeyRings()
    {
        var missing = CreateProvider(null);
        var malformed = CreateProvider("{not-json");
        var unknownProperty = CreateProvider(CreatePrivateJwks(("active", "active")).Replace(
            "\"status\":\"active\"",
            "\"status\":\"active\",\"unexpected\":true",
            StringComparison.Ordinal));
        var duplicateKid = CreateProvider(CreatePrivateJwks(("duplicate", "active"), ("duplicate", "retired")));
        var noActive = CreateProvider(CreatePrivateJwks(("retired", "retired")));
        var multipleActive = CreateProvider(CreatePrivateJwks(("one", "active"), ("two", "active")));

        missing.IsReady.Should().BeFalse();
        missing.FailureCode.Should().Be("missing_key_ring");
        malformed.IsReady.Should().BeFalse();
        unknownProperty.IsReady.Should().BeFalse();
        duplicateKid.IsReady.Should().BeFalse();
        noActive.FailureCode.Should().Be("invalid_active_key_count");
        multipleActive.FailureCode.Should().Be("invalid_active_key_count");
        multipleActive.GetPublicKeys().Should().BeEmpty();
    }

    [Test]
    public void KeyProviderRejectsNonCanonicalBase64UrlCoordinatesAndPrivateScalar()
    {
        var canonicalRing = CreatePrivateJwks(("active", "active"));

        CreateProvider(ReplaceWithNonCanonicalBase64Url(canonicalRing, "x")).IsReady.Should().BeFalse();
        CreateProvider(ReplaceWithNonCanonicalBase64Url(canonicalRing, "y")).IsReady.Should().BeFalse();
        CreateProvider(ReplaceWithNonCanonicalBase64Url(canonicalRing, "d")).IsReady.Should().BeFalse();
    }

    [Test]
    public void KeyProviderSelectsActiveAndRetiredKeysByKidAndRejectsUnknownKid()
    {
        var provider = CreateProvider(CreatePrivateJwks(("current", "active"), ("previous", "retired")));

        provider.IsReady.Should().BeTrue();
        provider.ActiveKeyId.Should().Be("current");
        using var current = provider.CreateActiveSigningKey();
        using var previous = provider.CreateSigningKey("previous");
        current.KeySize.Should().Be(256);
        previous.KeySize.Should().Be(256);
        var act = () => provider.CreateSigningKey("unknown");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ATProto OAuth client signing key is unavailable.");
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string privateRing,
        string? callbackPath = null,
        string publicUrl = "https://events.example.com")
    {
        return new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Atproto:PublicUrl", publicUrl);
            builder.UseSetting(AtprotoClientKeyProvider.ConfigurationKey, privateRing);
            if (callbackPath is not null)
            {
                builder.UseSetting("Atproto:CallbackPath", callbackPath);
            }
        });
    }

    private static AtprotoClientKeyProvider CreateProvider(string? privateRing)
    {
        return new AtprotoClientKeyProvider(Options.Create(new AtprotoClientKeyOptions
        {
            OAuthClientPrivateJwks = privateRing
        }));
    }

    private static string CreatePrivateJwks(params (string KeyId, string Status)[] specifications)
    {
        var keys = new List<object>();
        foreach (var specification in specifications)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = key.ExportParameters(true);
            keys.Add(new
            {
                kty = "EC",
                crv = "P-256",
                x = Base64Url(parameters.Q.X!),
                y = Base64Url(parameters.Q.Y!),
                d = Base64Url(parameters.D!),
                kid = specification.KeyId,
                use = "sig",
                alg = "ES256",
                status = specification.Status
            });
        }

        return JsonSerializer.Serialize(new
        {
            keys
        });
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string ReplaceWithNonCanonicalBase64Url(string ring, string propertyName)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        using var document = JsonDocument.Parse(ring);
        var canonical = document.RootElement.GetProperty("keys")[0].GetProperty(propertyName).GetString()!;
        var lastCharacterIndex = alphabet.IndexOf(canonical[^1]);
        var nonCanonical = canonical[..^1] + alphabet[lastCharacterIndex + 1];
        return ring.Replace(
            $"\"{propertyName}\":\"{canonical}\"",
            $"\"{propertyName}\":\"{nonCanonical}\"",
            StringComparison.Ordinal);
    }
}
