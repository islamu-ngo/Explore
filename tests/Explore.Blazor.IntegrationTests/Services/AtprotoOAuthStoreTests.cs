// ABOUTME: Proves protected ATProto OAuth state is issuer-bound, expiring, and atomically single-use.
// ABOUTME: Covers explicit single-node development storage and origin-bound opaque tenant handoffs.

using CarpaNet.OAuth.Crypto;
using CarpaNet.OAuth.Storage;
using Explore.Blazor.Authentication;
using Explore.Blazor.Client.Configuration;
using Explore.Blazor.Services.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackExchange.Redis;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class AtprotoOAuthStoreTests
{
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

    [Test]
    public async Task StateConsumeIsSingleUseAndPopulatesExactFlowBinding()
    {
        var fixture = CreateFixture("https://issuer.example/");
        await fixture.Store.StoreAsync("0123456789abcdef0123456789abcdef", CreateState());

        var results = await Task.WhenAll(
            fixture.Store.ConsumeAsync("0123456789abcdef0123456789abcdef"),
            fixture.Store.ConsumeAsync("0123456789abcdef0123456789abcdef"));

        await Assert.That(results.Count(result => result is not null)).IsEqualTo(1);
        await Assert.That(fixture.Context.Binding!.Seed.ExpectedDid).IsEqualTo("did:plc:alice");
        await Assert.That(fixture.Context.Binding.Issuer.AbsoluteUri).IsEqualTo("https://issuer.example/");
        await Assert.That(fixture.Context.Binding.Seed.TenantId).IsEqualTo(TenantId);
    }

    [Test]
    public async Task StateRoundTripsCanonicalActorTargetOnlyWhenTheCompletePairIsPresent()
    {
        var fixture = CreateFixture("https://issuer.example/");
        var canonicalActorId = Guid.NewGuid();
        var seed = CreateSeed(new Uri("https://events.example.com/")) with
        {
            CanonicalActorId = canonicalActorId,
            ExpectedCanonicalActorConcurrencyStamp = Guid.NewGuid()
        };
        var state = CreateState(seed);

        await fixture.Store.StoreAsync("0123456789abcdef0123456789abcdef", state);
        _ = await fixture.Store.ConsumeAsync("0123456789abcdef0123456789abcdef");

        await Assert.That(fixture.Context.Binding!.Seed.CanonicalActorId).IsEqualTo(canonicalActorId);
        await Assert.That(fixture.Context.Binding.Seed.ExpectedCanonicalActorConcurrencyStamp).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task StateRejectsTamperedOrHalfCanonicalActorTargetPair()
    {
        var fixture = CreateFixture("https://issuer.example/");
        var halfPair = CreateSeed(new Uri("https://events.example.com/")) with
        {
            CanonicalActorId = Guid.NewGuid()
        };
        var tampered = CreateState(halfPair);

        await Assert.That(async () => await fixture.Store.StoreAsync(
            "0123456789abcdef0123456789abcdef",
            tampered)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task IssuerSubstitutionConsumesAndRejectsState()
    {
        var fixture = CreateFixture("https://attacker.example/");
        await fixture.Store.StoreAsync("fedcba9876543210fedcba9876543210", CreateState());

        var substituted = await fixture.Store.ConsumeAsync("fedcba9876543210fedcba9876543210");
        fixture.HttpContext.Request.QueryString = new("?iss=https%3A%2F%2Fissuer.example%2F");
        var replay = await fixture.Store.ConsumeAsync("fedcba9876543210fedcba9876543210");

        await Assert.That(substituted).IsNull();
        await Assert.That(replay).IsNull();
    }

    [Test]
    public async Task MemoryStoreIsRejectedOutsideExplicitDevelopmentMode()
    {
        var options = Options.Create(CreateOptions(useMemory: false));
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var cache = new AtprotoAtomicCache([], environment, options, TimeProvider.System);

        await Assert.That(cache.IsReady).IsFalse();
        await Assert.That(async () => await cache.StoreAsync(
            "oauth-state-v1",
            "0123456789abcdef",
            [1],
            TimeSpan.FromMinutes(1),
            CancellationToken.None)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task HandoffRejectsHostSubstitutionAndReplay()
    {
        var options = Options.Create(CreateOptions(useMemory: true));
        options.Value.TenantOrigins.Add(new AtprotoTenantOrigin
        {
            Origin = "https://tenant.example/",
            TenantId = TenantId,
            TenantSlug = "default"
        });
        var environment = TestEnvironment();
        var cache = new AtprotoAtomicCache([], environment, options, TimeProvider.System);
        var resolver = CreateOriginResolver(options, environment);
        var handoffs = new AtprotoTenantSessionHandoffStore(
            cache,
            EphemeralDataProtection(),
            resolver,
            options,
            TimeProvider.System);
        var seed = CreateSeed(new Uri("https://tenant.example/"));
        var session = new AtprotoBffSessionResult(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            seed.ExpectedDid,
            seed.Classification,
            "opaque-platform-token",
            DateTimeOffset.UtcNow.AddMinutes(5));
        var code = await handoffs.CreateAsync(seed, session, CancellationToken.None);
        var wrongHost = CreateRequest("https", "other.example");
        var rightHost = CreateRequest("https", "tenant.example");

        var substituted = await handoffs.ConsumeAsync(code, wrongHost, CancellationToken.None);
        var replay = await handoffs.ConsumeAsync(code, rightHost, CancellationToken.None);

        await Assert.That(substituted).IsNull();
        await Assert.That(replay).IsNull();
    }

    [Test]
    public async Task HandoffPreservesCompleteCanonicalActorTargetPair()
    {
        var options = Options.Create(CreateOptions(useMemory: true));
        options.Value.TenantOrigins.Add(new AtprotoTenantOrigin
        {
            Origin = "https://tenant.example/",
            TenantId = TenantId,
            TenantSlug = "default"
        });
        var environment = TestEnvironment();
        var cache = new AtprotoAtomicCache([], environment, options, TimeProvider.System);
        var handoffs = new AtprotoTenantSessionHandoffStore(
            cache,
            EphemeralDataProtection(),
            CreateOriginResolver(options, environment),
            options,
            TimeProvider.System);
        var canonicalActorId = Guid.NewGuid();
        var seed = CreateSeed(new Uri("https://tenant.example/")) with
        {
            CanonicalActorId = canonicalActorId,
            ExpectedCanonicalActorConcurrencyStamp = Guid.NewGuid()
        };
        var session = new AtprotoBffSessionResult(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            seed.ExpectedDid,
            seed.Classification,
            "opaque-platform-token",
            DateTimeOffset.UtcNow.AddMinutes(5),
            canonicalActorId,
            seed.ExpectedCanonicalActorConcurrencyStamp);
        var code = await handoffs.CreateAsync(seed, session, CancellationToken.None);

        var handoff = await handoffs.ConsumeAsync(code, CreateRequest("https", "tenant.example"), CancellationToken.None);

        await Assert.That(handoff!.Seed.CanonicalActorId).IsEqualTo(canonicalActorId);
        await Assert.That(handoff.Session.ExpectedCanonicalActorConcurrencyStamp).IsEqualTo(seed.ExpectedCanonicalActorConcurrencyStamp);
    }

    private static StoreFixture CreateFixture(string callbackIssuer)
    {
        var options = Options.Create(CreateOptions(useMemory: true));
        var environment = TestEnvironment();
        var context = new AtprotoOAuthFlowContext();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new("events.example.com");
        httpContext.Request.QueryString = new($"?iss={Uri.EscapeDataString(callbackIssuer)}");
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var resolver = CreateOriginResolver(options, environment);
        var store = new CacheBackedOAuthStateStore(
            new AtprotoAtomicCache([], environment, options, TimeProvider.System),
            EphemeralDataProtection(),
            context,
            accessor,
            resolver,
            options,
            TimeProvider.System);
        return new(store, context, httpContext);
    }

    private static OAuthStateData CreateState(AtprotoOAuthFlowSeed? seed = null) => new()
    {
        Issuer = "https://issuer.example/",
        PdsUrl = "https://pds.example/",
        AppState = CacheBackedOAuthStateStore.EncodeAppState(seed ?? CreateSeed(new Uri("https://events.example.com/"))),
        Verifier = "verifier",
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(4)
    };

    private static AtprotoOAuthFlowSeed CreateSeed(Uri origin) => new(
        "did:plc:alice",
        new("https://pds.example/"),
        TenantId,
        "default",
        origin,
        "/events",
        "oauth-active",
        "person");

    private static AtprotoAuthenticationOptions CreateOptions(bool useMemory) => new()
    {
        PublicUrl = "https://events.example.com/",
        CallbackPath = "/signin-atproto",
        UseSingleNodeMemoryStore = useMemory,
        StateLifetimeSeconds = 300,
        HandoffLifetimeSeconds = 60
    };

    private static AtprotoTenantOriginResolver CreateOriginResolver(
        IOptions<AtprotoAuthenticationOptions> options,
        IWebHostEnvironment environment) => new(
        options,
        Options.Create(new TenantConfiguration
        {
            DefaultTenantId = TenantId,
            DefaultTenant = "default"
        }),
        environment);

    private static IWebHostEnvironment TestEnvironment()
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns("Testing");
        return environment;
    }

    private static IDataProtectionProvider EphemeralDataProtection() =>
        DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), $"atproto-tests-{Guid.NewGuid():N}"));

    private static HttpRequest CreateRequest(string scheme, string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new(host);
        return context.Request;
    }

    private sealed record StoreFixture(
        CacheBackedOAuthStateStore Store,
        AtprotoOAuthFlowContext Context,
        DefaultHttpContext HttpContext);
}
