// ABOUTME: Tests capability-aware ATProto Jetstream readiness without exposing tenant or DID identities.
// ABOUTME: Proves dormant empty configuration is healthy while enabled empty configuration fails closed.

using Explore.Infrastructure.HealthChecks;
using Explore.Infrastructure.Services.Federation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Federation;

public sealed class AtprotoJetstreamReadinessHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_DormantCapabilityWithEmptyAllowlistIsHealthy()
    {
        IAtprotoJetstreamRuntimeStore store = StoreWithEnabledTenants([]);
        var healthCheck = CreateHealthCheck(store, []);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["capabilityEnabled"]).IsEqualTo(false);
        await Assert.That(result.Data["allowlistConfigured"]).IsEqualTo(false);
    }

    [Test]
    public async Task CheckHealthAsync_EnabledCapabilityWithEmptyAllowlistIsUnhealthyAndBounded()
    {
        IAtprotoJetstreamRuntimeStore store = StoreWithEnabledTenants([Guid.CreateVersion7()]);
        var healthCheck = CreateHealthCheck(store, []);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Description).Contains("curated DID allowlist");
        await Assert.That(result.Data.Keys).IsEquivalentTo(["capabilityEnabled", "allowlistConfigured"]);
        await Assert.That(result.Data.Values).DoesNotContain(value => value is Guid or string);
    }

    [Test]
    public async Task CheckHealthAsync_EnabledCapabilityWithCuratedAllowlistIsHealthy()
    {
        IAtprotoJetstreamRuntimeStore store = StoreWithEnabledTenants([Guid.CreateVersion7()]);
        var healthCheck = CreateHealthCheck(store, ["did:plc:curated-owner"]);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["capabilityEnabled"]).IsEqualTo(true);
        await Assert.That(result.Data["allowlistConfigured"]).IsEqualTo(true);
    }

    private static AtprotoJetstreamReadinessHealthCheck CreateHealthCheck(
        IAtprotoJetstreamRuntimeStore store,
        string[] allowedDids) => new(
            store,
            Options.Create(new AtprotoJetstreamOptions { AllowedDids = allowedDids }));

    private static IAtprotoJetstreamRuntimeStore StoreWithEnabledTenants(IReadOnlyList<Guid> tenantIds)
    {
        IAtprotoJetstreamRuntimeStore store = Substitute.For<IAtprotoJetstreamRuntimeStore>();
        store.ResolveEnabledTenantIdsAsync(Arg.Any<CancellationToken>()).Returns(tenantIds);
        return store;
    }
}
