// ABOUTME: Verifies successful fenced Jetstream commits evict public discovery caches through the host boundary.
// ABOUTME: Ensures rejected stale writes cannot invalidate otherwise valid cached discovery responses.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Infrastructure.Services.Federation;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Federation;

public sealed class AtprotoJetstreamRuntimeStoreTests
{
    [Test]
    public async Task SuccessfulApplyInvalidatesDiscoveryCacheAfterRepositoryCommit()
    {
        IAtprotoJetstreamRepository repository = Substitute.For<IAtprotoJetstreamRepository>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        repository.TryApplyAndAdvanceAsync(Arg.Any<AtprotoJetstreamApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(true);
        AtprotoJetstreamRuntimeStore store = CreateStore(repository, invalidator);

        bool applied = await store.TryApplyAndAdvanceAsync(Request(affectsDiscovery: true), CancellationToken.None);

        await Assert.That(applied).IsTrue();
        await invalidator.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RejectedApplyDoesNotInvalidateDiscoveryCache()
    {
        IAtprotoJetstreamRepository repository = Substitute.For<IAtprotoJetstreamRepository>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        repository.TryApplyAndAdvanceAsync(Arg.Any<AtprotoJetstreamApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(false);
        AtprotoJetstreamRuntimeStore store = CreateStore(repository, invalidator);

        bool applied = await store.TryApplyAndAdvanceAsync(Request(affectsDiscovery: true), CancellationToken.None);

        await Assert.That(applied).IsFalse();
        await invalidator.DidNotReceiveWithAnyArgs().InvalidateAsync(default);
    }

    [Test]
    public async Task SuccessfulUnrelatedApplyDoesNotInvalidateDiscoveryCache()
    {
        IAtprotoJetstreamRepository repository = Substitute.For<IAtprotoJetstreamRepository>();
        IAtprotoDiscoveryCacheInvalidator invalidator = Substitute.For<IAtprotoDiscoveryCacheInvalidator>();
        repository.TryApplyAndAdvanceAsync(Arg.Any<AtprotoJetstreamApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(true);
        AtprotoJetstreamRuntimeStore store = CreateStore(repository, invalidator);

        bool applied = await store.TryApplyAndAdvanceAsync(Request(affectsDiscovery: false), CancellationToken.None);

        await Assert.That(applied).IsTrue();
        await invalidator.DidNotReceiveWithAnyArgs().InvalidateAsync(default);
    }

    private static AtprotoJetstreamRuntimeStore CreateStore(
        IAtprotoJetstreamRepository repository,
        IAtprotoDiscoveryCacheInvalidator invalidator)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => repository);
        services.AddScoped(_ => invalidator);
        ServiceProvider provider = services.BuildServiceProvider();
        return new AtprotoJetstreamRuntimeStore(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private static AtprotoJetstreamApplyRequest Request(bool affectsDiscovery)
    {
        var claim = new AtprotoJetstreamClaim(
            Guid.CreateVersion7(),
            "wss://jetstream.example/subscribe",
            41,
            Guid.CreateVersion7(),
            7);
        return new AtprotoJetstreamApplyRequest(
            claim,
            41,
            42,
            null,
            [],
            null,
            DateTime.UtcNow,
            EventProjectionInvalidation: affectsDiscovery
                ? new AtprotoEventProjectionInvalidation(
                    "did:plc:discovery",
                    AtprotoJetstreamConstants.EventCollection,
                    "event-record",
                    42)
                : null);
    }
}
